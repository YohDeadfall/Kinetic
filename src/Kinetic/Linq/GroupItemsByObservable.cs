using System;
using System.Collections.Generic;
using Kinetic.Runtime;

namespace Kinetic.Linq;

public readonly struct GroupItemsByObservable<TOperator, TSource, TKey, TResult> : IOperator<ListChange<TResult>>
    where TOperator : IOperator<ListChange<TSource>>
{
    private readonly TOperator _source;
    private readonly IEqualityComparer<TKey>? _keyComparer;
    private readonly Func<TSource, IObservable<TKey>> _keySelector;
    private readonly Func<IGrouping<TKey, ListChange<TSource>>, TResult> _resultSelector;

    public GroupItemsByObservable(
        TOperator source,
        Func<TSource, IObservable<TKey>> keySelector,
        Func<IGrouping<TKey, ListChange<TSource>>, TResult> resultSelector,
        IEqualityComparer<TKey>? comparer)
    {
        _source = source.ThrowIfArgumentNull();
        _keyComparer = comparer;
        _keySelector = keySelector.ThrowIfArgumentNull();
        _resultSelector = resultSelector.ThrowIfArgumentNull();
    }

    public TBox Build<TBox, TBoxFactory, TContinuation>(in TBoxFactory boxFactory, TContinuation continuation)
        where TBoxFactory : struct, IStateMachineBoxFactory<TBox>
        where TContinuation : struct, IStateMachine<ListChange<TResult>>
    {
        return _source.Build<
            TBox,
            TBoxFactory,
            GroupItemsByStateMachine<
                TransformItemsStateMachine<
                    TContinuation,
                    FuncTransform<IGrouping<TKey, ListChange<TSource>>, TResult>,
                    IGrouping<TKey, ListChange<TSource>>,
                    TResult>,
                GroupItemsByDynamicState<TKey, TSource>,
                GroupItemsByDynamicState<TKey, TSource>.Manager,
                TSource,
                TKey>>(
            boxFactory, new(new(continuation, new(_resultSelector)), new(_keySelector), _keyComparer));
    }
}