using System;
using System.Runtime.CompilerServices;

namespace Kinetic.Runtime;

internal abstract class PublishingBox<TSource, TResult, TStateMachine> : StateMachineBox<TSource, TStateMachine>
    where TStateMachine : struct, IStateMachine<TSource>
{
    protected PublishingBox(in TStateMachine stateMachine) :
        base(stateMachine)
    {
    }

    protected abstract void Complete();

    protected abstract void Error(Exception error);

    protected abstract void Next(TResult value);

    private struct StateMachineP : IStateMachine<TResult>
    {
        private PublishingBox<TSource, TResult, TStateMachine>? _box;

        public StateMachineBox Box =>
            _box ?? throw new InvalidOperationException();

        public StateMachineReference<TResult> Reference =>
            StateMachineReference<TResult>.Create<StateMachineP>(ref this);

        public StateMachineReference? Continuation =>
            null;

        public void Dispose()
        {
        }

        public void Initialize(StateMachineBox box)
        {
            // Safe as only the box can create that state machine.
            _box = Unsafe.As<PublishingBox<TSource, TResult, TStateMachine>>(box);
        }

        public void OnCompleted() =>
            _box?.Complete();

        public void OnError(Exception error) =>
            _box?.Error(error);

        public void OnNext(TResult value) =>
            _box?.Next(value);
    }
}
