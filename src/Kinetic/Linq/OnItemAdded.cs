using System;
using Kinetic.Runtime;

namespace Kinetic.Linq;

public readonly struct OnItemAdded<TOperator, TSource> : IOperator<ListChange<TSource>>
    where TOperator : IOperator<ListChange<TSource>>
{
    private readonly TOperator _source;
    private readonly Action<TSource> _onAdded;

    public OnItemAdded(TOperator source, Action<TSource> onAdded)
    {
        _source = source.ThrowIfNull();
        _onAdded = onAdded.ThrowIfNull();
    }

    public TBox Build<TBox, TBoxFactory, TContinuation>(in TBoxFactory boxFactory, TContinuation continuation)
        where TBoxFactory : struct, IStateMachineBoxFactory<TBox>
        where TContinuation : struct, IStateMachine<ListChange<TSource>>
    {
        return _source.Build<TBox, TBoxFactory, OnItemAddedStateMachine<TContinuation, TSource>>(
            boxFactory, new(continuation, _onAdded));
    }
}