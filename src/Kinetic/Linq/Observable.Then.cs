using System;
using Kinetic.Linq.StateMachines;

namespace Kinetic.Linq;

public static partial class Observable
{
    public static ObserverBuilder<TResult> Then<TSource, TResult>(this ObserverBuilder<TSource> source, Func<TSource, IObservable<TResult>> selector) =>
        source.ContinueWith<ThenStateMachineFactory<TSource, TResult>, TResult>(new(selector));

    public static ObserverBuilder<TResult> Then<TSource, TResult>(this IObservable<TSource> source, Func<TSource, IObservable<TResult>> selector) =>
        source.ToBuilder().Then(selector);

    private readonly struct ThenStateMachineFactory<TSource, TResult> : IObserverStateMachineFactory<TSource, TResult>
    {
        private readonly Func<TSource, IObservable<TResult>> _selector;

        public ThenStateMachineFactory(Func<TSource, IObservable<TResult>> selector) => _selector = selector;

        public void Create<TContinuation>(in TContinuation continuation, ObserverStateMachine<TSource> source)
            where TContinuation : struct, IObserverStateMachine<TResult>
        {
            source.ContinueWith(new ThenStateMachine<TContinuation, TSource, TResult>(continuation, _selector));
        }
    }

    private struct ThenStateMachine<TContinuation, TSource, TResult> : IObserverStateMachine<TSource>
        where TContinuation : struct, IObserverStateMachine<TResult>
    {
        private TContinuation _continuation;
        private IDisposable? _subscription;
        private readonly Func<TSource, IObservable<TResult>> _selector;

        public ThenStateMachine(TContinuation continuation, Func<TSource, IObservable<TResult>> selector)
        {
            _continuation = continuation;
            _selector = selector;
        }

        public ObserverStateMachineBox Box =>
            _continuation.Box;

        public void Initialize(ObserverStateMachineBox box) =>
            _continuation.Initialize(box);

        public void Dispose()
        {
            _subscription?.Dispose();
            _continuation.Dispose();
        }

        public void OnNext(TSource value)
        {
            try
            {
                _subscription?.Dispose();
                _subscription = _selector(value) is { } observable
                    ? observable.Subscribe(ref _continuation)
                    : null;
            }
            catch (Exception error)
            {
                _continuation.OnError(error);
            }
        }

        public void OnError(Exception error) =>
            _continuation.OnError(error);

        public void OnCompleted()
        {
            if (_subscription is null)
            {
                _continuation.OnCompleted();
            }
        }
    }
}