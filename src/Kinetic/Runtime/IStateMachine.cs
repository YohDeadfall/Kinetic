using System;

namespace Kinetic.Runtime;

public interface IStateMachine<T> : IObserver<T>, IDisposable
{
    StateMachineBox Box { get; }
    StateMachineReference<T> Reference { get; }
    StateMachineReference? Continuation { get; }

    void Initialize(StateMachineBox box);
}