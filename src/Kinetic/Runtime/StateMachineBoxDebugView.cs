using System.Collections.Generic;
using System.Diagnostics;

namespace Kinetic.Runtime;

internal sealed class StateMachineBoxDebugView<T, TStateMachine>
    where TStateMachine : struct, IEntryStateMachine<T>
{
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public IReadOnlyList<StateMachineReference> Items { get; }

    public StateMachineBoxDebugView(StateMachineBox<T, TStateMachine> box)
    {
        var items = new List<StateMachineReference>();
        var reference = box.StateMachine.Reference as StateMachineReference;
        while (reference is { })
        {
            items.Add(reference);
            reference = reference.Continuation;
        }

        Items = items.ToArray();
    }
}