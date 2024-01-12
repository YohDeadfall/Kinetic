using System;
using Kinetic.Linq.StateMachines;

namespace Kinetic.Linq;

public static partial class Observable
{
    public static IDisposable Subscribe<T>(this IObservable<T> source) =>
        source.ToBuilder().Subscribe();

    public static IDisposable Subscribe<T>(this IObservable<T> source, Action<T> onNext) =>
        source.ToBuilder().Subscribe(onNext);

    public static IDisposable Subscribe<T>(this IObservable<T> source, Action<T> onNext, Action<Exception> onError) =>
        source.ToBuilder().Subscribe(onNext, onError);

    public static IDisposable Subscribe<T>(this IObservable<T> source, Action<T> onNext, Action onCompleted) =>
        source.ToBuilder().Subscribe(onNext, onCompleted);

    public static IDisposable Subscribe<T>(this IObservable<T> source, Action<T> onNext, Action<Exception> onError, Action onCompleted) =>
        source.ToBuilder().Subscribe(onNext, onError, onCompleted);

    public static IDisposable Subscribe<T>(this ObserverBuilder<T> source) =>
        source.Build<SubscribeStateMachine<T>, SubscribeBoxFactory, IDisposable>(
            continuation: new(),
            factory: new());

    public static IDisposable Subscribe<T>(this ObserverBuilder<T> source, Action<T> onNext) =>
        source.Do(onNext).Subscribe();

    public static IDisposable Subscribe<T>(this ObserverBuilder<T> source, Action<T> onNext, Action<Exception> onError) =>
        source.Do(onNext, onError).Subscribe();

    public static IDisposable Subscribe<T>(this ObserverBuilder<T> source, Action<T> onNext, Action onCompleted) =>
        source.Do(onNext, onCompleted).Subscribe();

    public static IDisposable Subscribe<T>(this ObserverBuilder<T> source, Action<T> onNext, Action<Exception> onError, Action onCompleted) =>
        source.Do(onNext, onError, onCompleted).Subscribe();

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
        private ObserverStateMachineBox? _box;

        public ObserverStateMachineBox Box =>
            _box ?? throw new InvalidOperationException();

        public void Initialize(ObserverStateMachineBox box) =>
            _box = box;

        public void Dispose() =>
            _box = null;

        public void OnCompleted() { }
        public void OnError(Exception error) { }
        public void OnNext(T value) { }
    }
}