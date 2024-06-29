using System;

namespace Kinetic.Linq.StateMachines;

public interface IStateMachine<T> : IObserver<T>, IDisposable
{
    StateMachineBox Box { get; }

    void Initialize(StateMachineBox box);
}