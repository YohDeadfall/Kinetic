using System.Collections;
using System.Collections.Generic;

namespace Kinetic.Linq.StateMachines;

internal sealed class ListProxyStateMachine<T, TStateMachine> : StateMachine<ListChange<T>, TStateMachine>, IReadOnlyList<T>
    where TStateMachine : struct, IStateMachine<ListChange<T>>
{
    private readonly IReadOnlyList<T> _list;

    public ListProxyStateMachine(ref TStateMachine stateMachine, IReadOnlyList<T> list) :
        base(ref stateMachine) => _list = list;

    public ListProxyStateMachine(StateMachineReference<ListChange<T>, TStateMachine> stateMachine, IReadOnlyList<T> list) :
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