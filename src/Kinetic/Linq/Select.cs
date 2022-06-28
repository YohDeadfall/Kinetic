using System;
using Kinetic.Linq.StateMachines;

namespace Kinetic.Linq;

public static partial class Observable
{
    public static ObserverBuilder<TResult> Select<TSource, TResult>(this in ObserverBuilder<TSource> source, Func<TSource, TResult> selector) =>
        source.ContinueWith<SelectStateMachineFactory<TSource, TResult>, TResult>(new(selector));

    public static ObserverBuilder<TResult> Select<TSource, TResult>(this IObservable<TSource> source, Func<TSource, TResult> selector) =>
        source.ToBuilder().Select(selector);

    private readonly struct SelectStateMachineFactory<TSource, TResult> : IObserverStateMachineFactory<TSource, TResult>
    {
        private readonly Func<TSource, TResult> _selector;

        public SelectStateMachineFactory(Func<TSource, TResult> selector) => _selector = selector;

        public void Create<TContinuation>(in TContinuation continuation, ObserverStateMachine<TSource> source)
            where TContinuation : struct, IObserverStateMachine<TResult>
        {
            source.ContinueWith(new SelectStateMachine<TContinuation, TSource, TResult>(continuation, _selector));
        }
    }

    private struct SelectStateMachine<TContinuation, TSource, TResult> : IObserverStateMachine<TSource>
        where TContinuation : struct, IObserverStateMachine<TResult>
    {
        private TContinuation _continuation;
        private Func<TSource, TResult> _selector;

        public SelectStateMachine(TContinuation continuation, Func<TSource, TResult> selector)
        {
            _continuation = continuation;
            _selector = selector;
        }

        public void Initialize(IObserverStateMachineBox box) => _continuation.Initialize(box);
        public void Dispose() => _continuation.Dispose();

        public void OnNext(TSource value)
        {
            try
            {
                _continuation.OnNext(_selector(value));
            }
            catch (Exception error)
            {
                _continuation.OnError(error);
            }
        }

        public void OnError(Exception error) => _continuation.OnError(error);
        public void OnCompleted() => _continuation.OnCompleted();
    }
}