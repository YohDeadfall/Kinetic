using System;
using Kinetic.Runtime;

namespace Kinetic.Linq;

internal readonly struct ObserverFactory<TResult> : IStateMachineBoxFactory<IDisposable>
{
    public static IDisposable Create<TOperator>(TOperator source)
        where TOperator : IOperator<TResult>
    {
        return source.Build<IDisposable, ObserverFactory<TResult>, StateMachine>(
            new(), new());
    }

    public IDisposable Create<TSource, TStateMachine>(TStateMachine stateMachine)
        where TStateMachine : struct, IStateMachine<TSource>
    {
        return new Box<TSource, TStateMachine>(stateMachine);
    }

    private sealed class Box<T, TStateMachine> : StateMachineBox<T, TStateMachine>, IDisposable
        where TStateMachine : struct, IStateMachine<T>
    {
        public Box(TStateMachine stateMachine) :
            base(stateMachine) =>
            StateMachine.Initialize(this);

        public void Dispose() =>
            StateMachine.Dispose();
    }

    private struct StateMachine : IStateMachine<TResult>
    {
        private StateMachineBox _box;

        public StateMachineBox Box =>
            _box ?? throw new InvalidOperationException();

        public StateMachineReference<TResult> Reference =>
            StateMachineReference<TResult>.Create(ref this);

        public StateMachineReference? Continuation =>
            null;

        public void Dispose() { }

        public void Initialize(StateMachineBox box) =>
            _box = box;

        public void OnCompleted() { }

        public void OnError(Exception error) { }

        public void OnNext(TResult value) { }
    }
}