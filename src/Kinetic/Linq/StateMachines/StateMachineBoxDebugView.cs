using System.Collections.Generic;
using System.Diagnostics;

namespace Kinetic.Linq.StateMachines;

internal sealed class StateMachineBoxDebugView<T, TStateMachine>
    where TStateMachine : struct, IStateMachine<T>
{
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public IReadOnlyList<StateMachine> Items { get; }

    public StateMachineBoxDebugView(StateMachineBox<T, TStateMachine> box)
    {
        var items = new List<StateMachine>();
        var reference = box.StateMachine.Reference as StateMachine;
        while (reference is { })
        {
            items.Add(reference);
            reference = reference.Continuation;
        }

        Items = items.ToArray();
    }
}