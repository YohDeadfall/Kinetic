using System;

namespace Kinetic.Linq.StateMachines;

public static partial class Observable
{
    public static IDisposable Subscribe<T, TStateMachine>(this IObservable<T> source, ref TStateMachine stateMachine)
        where TStateMachine : struct, IObserverStateMachine<T> =>
        source.ToBuilder().Subscribe(ref stateMachine);

    public static IDisposable Subscribe<T, TStateMachine>(this ObserverBuilder<T> source, ref TStateMachine stateMachine)
        where TStateMachine : struct, IObserverStateMachine<T> =>
        source.Build<SubscribeStateMachine<T, TStateMachine>, SubscribeBoxFactory, IDisposable>(
            continuation: new(new(ref stateMachine)),
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

    private struct SubscribeStateMachine<T, TStateMachine> : IObserverStateMachine<T>
        where TStateMachine : struct, IObserverStateMachine<T>
    {
        private readonly ObserverStateMachineReference<T, TStateMachine> _stateMachine;
        private ObserverStateMachineBox? _box;

        public SubscribeStateMachine(ObserverStateMachineReference<T, TStateMachine> stateMachine) =>
            _stateMachine = stateMachine;

        public ObserverStateMachineBox Box =>
            _box ?? throw new InvalidOperationException();

        public void Initialize(ObserverStateMachineBox box) =>
            _box = box;

        public void Dispose() { }

        public void OnCompleted() =>
            _stateMachine.Target.OnCompleted();

        public void OnError(Exception error) =>
            _stateMachine.Target.OnError(error);

        public void OnNext(T value) =>
            _stateMachine.Target.OnNext(value);
    }
}