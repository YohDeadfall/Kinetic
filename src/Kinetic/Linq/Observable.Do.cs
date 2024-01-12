using System;
using Kinetic.Linq.StateMachines;

namespace Kinetic.Linq;

public static partial class Observable
{
    public static ObserverBuilder<T> Do<T>(this IObservable<T> source, Action<T> onNext) =>
        source.ToBuilder().Do(onNext);

    public static ObserverBuilder<T> Do<T>(this IObservable<T> source, Action<T> onNext, Action<Exception> onError) =>
        source.ToBuilder().Do(onNext, onError);

    public static ObserverBuilder<T> Do<T>(this IObservable<T> source, Action<T> onNext, Action onCompleted) =>
        source.ToBuilder().Do(onNext, onCompleted);

    public static ObserverBuilder<T> Do<T>(this IObservable<T> source, Action<T> onNext, Action<Exception> onError, Action onCompleted) =>
        source.ToBuilder().Do(onNext, onError, onCompleted);

    public static ObserverBuilder<T> Do<T>(this ObserverBuilder<T> source, Action<T> onNext) =>
        source.ContinueWith<DoStateMachineFactory<T>, T>(new(onNext, onError: null, onCompleted: null));

    public static ObserverBuilder<T> Do<T>(this ObserverBuilder<T> source, Action<T> onNext, Action<Exception> onError) =>
        source.ContinueWith<DoStateMachineFactory<T>, T>(new(onNext, onError, onCompleted: null));

    public static ObserverBuilder<T> Do<T>(this ObserverBuilder<T> source, Action<T> onNext, Action onCompleted) =>
        source.ContinueWith<DoStateMachineFactory<T>, T>(new(onNext, onError: null, onCompleted));

    public static ObserverBuilder<T> Do<T>(this ObserverBuilder<T> source, Action<T> onNext, Action<Exception> onError, Action onCompleted) =>
        source.ContinueWith<DoStateMachineFactory<T>, T>(new(onNext, onError, onCompleted));

    private readonly struct DoStateMachineFactory<T> : IObserverStateMachineFactory<T, T>
    {
        private readonly Action<T>? _onNext;
        private readonly Action<Exception>? _onError;
        private readonly Action? _onCompleted;

        public DoStateMachineFactory(
            Action<T>? onNext,
            Action<Exception>? onError,
            Action? onCompleted)
        {
            _onNext = onNext;
            _onError = onError;
            _onCompleted = onCompleted;
        }

        public void Create<TContinuation>(in TContinuation continuation, ObserverStateMachine<T> source)
            where TContinuation : struct, IObserverStateMachine<T>
        {
            source.ContinueWith(new DoStateMachine<TContinuation, T>(continuation, _onNext, _onError, _onCompleted));
        }
    }

    private struct DoStateMachine<TContinuation, T> : IObserverStateMachine<T>
        where TContinuation : struct, IObserverStateMachine<T>
    {
        private TContinuation _continuation;
        private readonly Action<T>? _onNext;
        private readonly Action<Exception>? _onError;
        private readonly Action? _onCompleted;

        public DoStateMachine(
            in TContinuation continuation,
            Action<T>? onNext,
            Action<Exception>? onError,
            Action? onCompleted)
        {
            _continuation = continuation;
            _onNext = onNext;
            _onError = onError;
            _onCompleted = onCompleted;
        }

        public ObserverStateMachineBox Box =>
            _continuation.Box;

        public void Initialize(ObserverStateMachineBox box) =>
            _continuation.Initialize(box);

        public void Dispose() =>
            _continuation.Dispose();

        public void OnNext(T value)
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