using System;
using Kinetic.Linq.StateMachines;

namespace Kinetic.Linq;

public static partial class Observable
{
    public static IDisposable Subscribe<T>(this IObservable<T> source, Action<T> onNext) =>
        source.ToBuilder().Subscribe(onNext);

    public static IDisposable Subscribe<T>(this IObservable<T> source, Action<T> onNext, Action<Exception> onError) =>
        source.ToBuilder().Subscribe(onNext, onError);

    public static IDisposable Subscribe<T>(this IObservable<T> source, Action<T> onNext, Action onCompleted) =>
        source.ToBuilder().Subscribe(onNext, onCompleted);

    public static IDisposable Subscribe<T>(this IObservable<T> source, Action<T> onNext, Action<Exception> onError, Action onCompleted) =>
        source.ToBuilder().Subscribe(onNext, onError, onCompleted);

    public static IDisposable Subscribe<T>(this ObserverBuilder<T> source, Action<T> onNext) =>
        source.Build<Subscribe<T>.StateMachine, Subscribe<T>.BoxFactory, IDisposable>(
            continuation: new(onNext, onError: null, onCompleted: null),
            factory: new());

    public static IDisposable Subscribe<T>(this ObserverBuilder<T> source, Action<T> onNext, Action<Exception> onError) =>
        source.Build<Subscribe<T>.StateMachine, Subscribe<T>.BoxFactory, IDisposable>(
            continuation: new(onNext, onError, onCompleted: null),
            factory: new());

    public static IDisposable Subscribe<T>(this ObserverBuilder<T> source, Action<T> onNext, Action onCompleted) =>
        source.Build<Subscribe<T>.StateMachine, Subscribe<T>.BoxFactory, IDisposable>(
            continuation: new(onNext, onError: null, onCompleted),
            factory: new());

    public static IDisposable Subscribe<T>(this ObserverBuilder<T> source, Action<T> onNext, Action<Exception> onError, Action onCompleted) =>
        source.Build<Subscribe<T>.StateMachine, Subscribe<T>.BoxFactory, IDisposable>(
            continuation: new(onNext, onError, onCompleted),
            factory: new());

    public static IDisposable Subscribe<T, TStateMachine>(this ObserverBuilder<T> source, in TStateMachine stateMachine, ObserverStateMachineBox box)
        where TStateMachine : struct, IObserverStateMachine<T> =>
        box.Subscribe(source, stateMachine);
}

internal static class Subscribe<TResult>
{
    private sealed class Box<T, TStateMachine> : ObserverStateMachineBox<T, TStateMachine>, IDisposable
        where TStateMachine : struct, IObserverStateMachine<T>
    {
        public Box(in TStateMachine stateMachine) :
            base(stateMachine) => StateMachine.Initialize(this);

        public void Dispose() =>
            StateMachine.Dispose();
    }

    internal readonly struct BoxFactory : IObserverFactory<IDisposable>
    {
        public IDisposable Create<T, TStateMachine>(in TStateMachine stateMachine)
            where TStateMachine : struct, IObserverStateMachine<T> =>
            new Box<T, TStateMachine>(stateMachine);
    }

    internal struct StateMachine : IObserverStateMachine<TResult>
    {
        private readonly Action<TResult>? _onNext;
        private readonly Action<Exception>? _onError;
        private readonly Action? _onCompleted;
        private IDisposable? _subscription;

        public StateMachine(Action<TResult>? onNext, Action<Exception>? onError, Action? onCompleted)
        {
            _onNext = onNext;
            _onError = onError;
            _onCompleted = onCompleted;
            _subscription = null;
        }

        public void Dispose() =>
            _subscription = null;

        public void Initialize(ObserverStateMachineBox box) =>
            _subscription = (IDisposable) box;

        public void OnCompleted() => _onCompleted?.Invoke();
        public void OnError(Exception error) => _onError?.Invoke(error);
        public void OnNext(TResult value) => _onNext?.Invoke(value);
    }
}