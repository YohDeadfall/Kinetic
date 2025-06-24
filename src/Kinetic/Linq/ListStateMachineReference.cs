using System.Collections;
using System.Collections.Generic;
using Kinetic.Runtime;

namespace Kinetic.Linq;

internal sealed class ListStateMachineReference<T, TStateMachine> : StateMachineReference<ListChange<T>, TStateMachine>, IReadOnlyList<T>
    where TStateMachine : struct, IStateMachine<ListChange<T>>, IReadOnlyList<T>
{
    public ListStateMachineReference(ref TStateMachine stateMachine) :
        base(ref stateMachine)
    { }

    public ListStateMachineReference(StateMachineReference<ListChange<T>, TStateMachine> stateMachine) :
        base(stateMachine)
    { }

    public T this[int index] =>
        Target[index];

    public int Count =>
        Target.Count;

    public IEnumerator<T> GetEnumerator() =>
        Target.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() =>
        Target.GetEnumerator();
}