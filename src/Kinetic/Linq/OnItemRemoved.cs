using System;
using Kinetic.Runtime;

namespace Kinetic.Linq;

public readonly struct OnItemRemoved<TOperator, TSource> : IOperator<ListChange<TSource>>
    where TOperator : IOperator<ListChange<TSource>>
{
    private readonly TOperator _source;
    private readonly Action<TSource> _onRemoved;

    public OnItemRemoved(TOperator source, Action<TSource> onRemoved)
    {
        _source = source.ThrowIfNull();
        _onRemoved = onRemoved.ThrowIfNull();
    }

    public TBox Build<TBox, TBoxFactory, TContinuation>(in TBoxFactory boxFactory, TContinuation continuation)
        where TBoxFactory : struct, IStateMachineBoxFactory<TBox>
        where TContinuation : struct, IStateMachine<ListChange<TSource>>
    {
        return _source.Build<TBox, TBoxFactory, OnItemRemovedStateMachine<TContinuation, TSource>>(
            boxFactory, new(continuation, _onRemoved));
    }
}