using System.Diagnostics;

namespace Kinetic.Runtime;

internal sealed class StateMachineReferenceDebugView<T, TStateMachine>
    where TStateMachine : struct, IStateMachine<T>
{
    private readonly StateMachineValueReference<T, TStateMachine> _stateMachine;

    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public TStateMachine StateMachine =>
        _stateMachine.Target;

    public StateMachineReferenceDebugView(StateMachineReference<T, TStateMachine> stateMachine) =>
        _stateMachine = stateMachine.Ta;
}