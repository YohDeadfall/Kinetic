using System;
using System.Collections.Generic;
using Kinetic.Runtime;

namespace Kinetic.Linq;

public readonly struct OrderItemsByObservable<TOperator, TSource, TKey> : IOperator<ListChange<TSource>>
    where TOperator : IOperator<ListChange<TSource>>
{
    private readonly TOperator _source;
    private readonly Func<TSource, IObservable<TKey>> _keySelector;
    private readonly IComparer<TKey>? _keyComparer;

    public OrderItemsByObservable(TOperator source, Func<TSource, IObservable<TKey>> keySelector, IComparer<TKey>? comparer)
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
                OrderItemsByDynamicState<TKey, TSource>,
                OrderItemsByDynamicState<TKey, TSource>.Manager,
                TSource,
                TKey>>(
            boxFactory, new(continuation, new(_keySelector), _keyComparer));
    }
}