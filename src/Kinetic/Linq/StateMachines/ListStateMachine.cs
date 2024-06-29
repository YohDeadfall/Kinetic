using System.Collections;
using System.Collections.Generic;

namespace Kinetic.Linq.StateMachines;

public class ListStateMachine<T, TStateMachine> : StateMachine<ListChange<T>, TStateMachine>, IReadOnlyList<T>
    where TStateMachine : struct, IStateMachine<ListChange<T>>, IReadOnlyList<T>
{
    public ListStateMachine(ref TStateMachine stateMachine) :
        base(ref stateMachine)
    { }

    public ListStateMachine(StateMachineReference<ListChange<T>, TStateMachine> stateMachine) :
        base(stateMachine)
    { }

    public T this[int index] =>
        Reference.Target[index];

    public int Count =>
        Reference.Target.Count;

    public IEnumerator<T> GetEnumerator() =>
        Reference.Target.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() =>
        Reference.Target.GetEnumerator();
}