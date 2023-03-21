using System;
using Kinetic.Linq.StateMachines;

namespace Kinetic.Linq;

public static partial class Observable
{
    public static ObserverBuilder<TSource> Do<TSource>(this IObservable<TSource> source, Action<TSource> onNext) =>
        source.ToBuilder().Do(onNext);

    public static ObserverBuilder<TSource> Do<TSource>(this IObservable<TSource> source, Action<TSource> onNext, Action<Exception> onError) =>
        source.ToBuilder().Do(onNext, onError);

    public static ObserverBuilder<TSource> Do<TSource>(this IObservable<TSource> source, Action<TSource> onNext, Action onCompleted) =>
        source.ToBuilder().Do(onNext, onCompleted);

    public static ObserverBuilder<TSource> Do<TSource>(this IObservable<TSource> source, Action<TSource> onNext, Action<Exception> onError, Action onCompleted) =>
        source.ToBuilder().Do(onNext, onError, onCompleted);

    public static ObserverBuilder<TSource> Do<TSource>(this ObserverBuilder<TSource> source, Action<TSource> onNext) =>
        source.ContinueWith<DoStateMachineFactory<TSource>, TSource>(new(onNext, onError: null, onCompleted: null));

    public static ObserverBuilder<TSource> Do<TSource>(this ObserverBuilder<TSource> source, Action<TSource> onNext, Action<Exception> onError) =>
        source.ContinueWith<DoStateMachineFactory<TSource>, TSource>(new(onNext, onError, onCompleted: null));

    public static ObserverBuilder<TSource> Do<TSource>(this ObserverBuilder<TSource> source, Action<TSource> onNext, Action onCompleted) =>
        source.ContinueWith<DoStateMachineFactory<TSource>, TSource>(new(onNext, onError: null, onCompleted));

    public static ObserverBuilder<TSource> Do<TSource>(this ObserverBuilder<TSource> source, Action<TSource> onNext, Action<Exception> onError, Action onCompleted) =>
        source.ContinueWith<DoStateMachineFactory<TSource>, TSource>(new(onNext, onError, onCompleted));

    private struct DoStateMachineFactory<TSource> : IObserverStateMachineFactory<TSource, TSource>
    {
        private readonly Action<TSource>? _onNext;
        private readonly Action<Exception>? _onError;
        private readonly Action? _onCompleted;

        public DoStateMachineFactory(
            Action<TSource>? onNext,
            Action<Exception>? onError,
            Action? onCompleted)
        {
            _onNext = onNext;
            _onError = onError;
            _onCompleted = onCompleted;
        }

        public void Create<TContinuation>(in TContinuation continuation, ObserverStateMachine<TSource> source)
            where TContinuation : struct, IObserverStateMachine<TSource>
        {
            source.ContinueWith(new DoStateMachine<TContinuation, TSource>(continuation, _onNext, _onError, _onCompleted));
        }
    }

    private struct DoStateMachine<TContinuation, TSource> : IObserverStateMachine<TSource>
        where TContinuation : struct, IObserverStateMachine<TSource>
    {
        private TContinuation _continuation;
        private readonly Action<TSource>? _onNext;
        private readonly Action<Exception>? _onError;
        private readonly Action? _onCompleted;

        public DoStateMachine(
            in TContinuation continuation,
            Action<TSource>? onNext,
            Action<Exception>? onError,
            Action? onCompleted)
        {
            _continuation = continuation;
            _onNext = onNext;
            _onError = onError;
            _onCompleted = onCompleted;
        }

        public void Initialize(ObserverStateMachineBox box) => _continuation.Initialize(box);
        public void Dispose() => _continuation.Dispose();

        public void OnNext(TSource value)
        {
            _onNext?.Invoke(value);
            _continuation.OnNext(value);
        }

        public void OnError(Exception error)
        {
            _onError?.Invoke(error);
            _continuation.OnError(error);
        }

        public void OnCompleted()
        {
            _onCompleted?.Invoke();
            _continuation.OnCompleted();
        }
    }
}