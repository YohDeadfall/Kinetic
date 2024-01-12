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
        source.Build<SubscribeStateMachine<T>, SubscribeBoxFactory, IDisposable>(
            continuation: new(onNext, onError: null, onCompleted: null),
            factory: new());

    public static IDisposable Subscribe<T>(this ObserverBuilder<T> source, Action<T> onNext, Action<Exception> onError) =>
        source.Build<SubscribeStateMachine<T>, SubscribeBoxFactory, IDisposable>(
            continuation: new(onNext, onError, onCompleted: null),
            factory: new());

    public static IDisposable Subscribe<T>(this ObserverBuilder<T> source, Action<T> onNext, Action onCompleted) =>
        source.Build<SubscribeStateMachine<T>, SubscribeBoxFactory, IDisposable>(
            continuation: new(onNext, onError: null, onCompleted),
            factory: new());

    public static IDisposable Subscribe<T>(this ObserverBuilder<T> source, Action<T> onNext, Action<Exception> onError, Action onCompleted) =>
        source.Build<SubscribeStateMachine<T>, SubscribeBoxFactory, IDisposable>(
            continuation: new(onNext, onError, onCompleted),
            factory: new());

    private sealed class SubscribeBox<T, TStateMachine> : ObserverStateMachineBox<T, TStateMachine>, IDisposable
        where TStateMachine : struct, IObserverStateMachine<T>
    {
        public SubscribeBox(in TStateMachine stateMachine) :
            base(stateMachine) => StateMachine.Initialize(this);

        public void Dispose() =>
            StateMachine.Dispose();
    }

    private readonly struct SubscribeBoxFactory : IObserverFactory<IDisposable>
    {
        public IDisposable Create<T, TStateMachine>(in TStateMachine stateMachine)
            where TStateMachine : struct, IObserverStateMachine<T> =>
            new SubscribeBox<T, TStateMachine>(stateMachine);
    }

    private struct SubscribeStateMachine<T> : IObserverStateMachine<T>
    {
        private readonly Action<T>? _onNext;
        private readonly Action<Exception>? _onError;
        private readonly Action? _onCompleted;
        private ObserverStateMachineBox? _box;

        public SubscribeStateMachine(Action<T>? onNext, Action<Exception>? onError, Action? onCompleted)
        {
            _onNext = onNext;
            _onError = onError;
            _onCompleted = onCompleted;
        }

        public ObserverStateMachineBox Box =>
            _box ?? throw new InvalidOperationException();

        public void Initialize(ObserverStateMachineBox box) =>
            _box = box;

        public void Dispose() =>
            _box = null;

        public void OnCompleted() =>
            _onCompleted?.Invoke();

        public void OnError(Exception error) =>
            _onError?.Invoke(error);

        public void OnNext(T value) =>
            _onNext?.Invoke(value);
    }
}