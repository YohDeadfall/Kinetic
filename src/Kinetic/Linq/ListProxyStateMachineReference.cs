using System.Collections;
using System.Collections.Generic;
using Kinetic.Runtime;

namespace Kinetic.Linq;

internal sealed class ListProxyStateMachineReference<T, TStateMachine> : StateMachineReference<ListChange<T>, TStateMachine>, IReadOnlyList<T>
    where TStateMachine : struct, IStateMachine<ListChange<T>>
{
    private readonly IReadOnlyList<T> _list;

    public ListProxyStateMachineReference(ref TStateMachine stateMachine, IReadOnlyList<T> list) :
        base(ref stateMachine) => _list = list;

    public ListProxyStateMachineReference(StateMachineReference<ListChange<T>, TStateMachine> stateMachine, IReadOnlyList<T> list) :
        base(stateMachine) => _list = list;

    public T this[int index] =>
        _list[index];

    public int Count =>
        _list.Count;

    public IEnumerator<T> GetEnumerator() =>
        _list.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() =>
        _list.GetEnumerator();
}