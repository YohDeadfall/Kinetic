using System.Diagnostics;

namespace Kinetic.Linq.StateMachines;

internal sealed class StateMachineDebugView<T, TStateMachine>
    where TStateMachine : struct, IStateMachine<T>
{
    private readonly StateMachine<T, TStateMachine> _stateMachine;

    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public TStateMachine StateMachine =>
        _stateMachine.Reference.Target;

    public StateMachineDebugView(StateMachine<T, TStateMachine> stateMachine) =>
        _stateMachine = stateMachine;
}