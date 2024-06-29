using System;

namespace Kinetic.Linq.StateMachines;

public static partial class Observable
{
    public static IDisposable Subscribe<T, TStateMachine>(this IObservable<T> source, ref TStateMachine stateMachine)
        where TStateMachine : struct, IStateMachine<T> =>
        source.ToBuilder().Subscribe(ref stateMachine);

    public static IDisposable Subscribe<T, TStateMachine>(this ObserverBuilder<T> source, ref TStateMachine stateMachine)
        where TStateMachine : struct, IStateMachine<T> =>
        source.Build<SubscribeStateMachine<T, TStateMachine>, SubscribeBoxFactory, IDisposable>(
            continuation: new(new(ref stateMachine)),
            factory: new());

    private sealed class SubscribeBox<T, TStateMachine> : StateMachineBox<T, TStateMachine>, IDisposable
        where TStateMachine : struct, IStateMachine<T>
    {
        public SubscribeBox(in TStateMachine stateMachine) :
            base(stateMachine) => StateMachine.Initialize(this);

        public void Dispose() =>
            StateMachine.Dispose();
    }

    private readonly struct SubscribeBoxFactory : IStateMachineBoxFactory<IDisposable>
    {
        public IDisposable Create<T, TStateMachine>(in TStateMachine stateMachine)
            where TStateMachine : struct, IStateMachine<T> =>
            new SubscribeBox<T, TStateMachine>(stateMachine);
    }

    private struct SubscribeStateMachine<T, TStateMachine> : IStateMachine<T>
        where TStateMachine : struct, IStateMachine<T>
    {
        private readonly StateMachineReference<T, TStateMachine> _stateMachine;
        private StateMachineBox? _box;

        public SubscribeStateMachine(StateMachineReference<T, TStateMachine> stateMachine) =>
            _stateMachine = stateMachine;

        public StateMachineBox Box =>
            _box ?? throw new InvalidOperationException();

        public StateMachine<T> Reference =>
            StateMachine<T>.Create(ref this);

        public StateMachine? Continuation =>
            null;

        public void Initialize(StateMachineBox box) =>
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