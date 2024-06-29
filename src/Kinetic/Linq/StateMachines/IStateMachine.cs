using System;

namespace Kinetic.Linq.StateMachines;

public interface IStateMachine<T> : IObserver<T>, IDisposable
{
    StateMachineBox Box { get; }
    StateMachine<T> Reference { get; }
    StateMachine? Continuation { get; }

    void Initialize(StateMachineBox box);
}