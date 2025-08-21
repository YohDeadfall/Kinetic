using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Kinetic.Runtime;

namespace Kinetic.Linq;

[StructLayout(LayoutKind.Auto)]
public readonly struct OrderItemsBy<TOperator, TSource, TKey> : IOperator<ListChange<TSource>>
    where TOperator : IOperator<ListChange<TSource>>
{
    private readonly TOperator _source;
    private readonly Func<TSource, TKey> _keySelector;
    private readonly IComparer<TKey>? _keyComparer;

    public OrderItemsBy(TOperator source, Func<TSource, TKey> keySelector, IComparer<TKey>? comparer)
    {
        _source = source.ThrowIfArgumentNull();
        _keySelector = keySelector.ThrowIfArgumentNull();
        _keyComparer = comparer;
    }

    public TBox Build<TBox, TBoxFactory, TContinuation>(in TBoxFactory boxFactory, TContinuation continuation)
        where TBoxFactory : struct, IStateMachineBoxFactory<TBox>
        where TContinuation : struct, IStateMachine<ListChange<TSource>>
    {
        return _source.Build<
            TBox,
            TBoxFactory,
            OrderItemsByStateMachine<
                TContinuation,
                OrderItemsByStaticState<TKey>,
                OrderItemsByStaticState<TKey>.Manager<TSource>,
                TSource,
                TKey>>(
            boxFactory, new(continuation, new(_keySelector), _keyComparer));
    }
}