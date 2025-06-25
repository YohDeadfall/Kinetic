using System;
using System.Collections.Generic;
using Kinetic.Runtime;

namespace Kinetic.Linq;

public readonly struct GroupItemsBy<TOperator, TSource, TKey, TResult> : IOperator<ListChange<TResult>>
    where TOperator : IOperator<ListChange<TSource>>
{
    private readonly TOperator _source;
    private readonly IEqualityComparer<TKey>? _keyComparer;
    private readonly Delegate _keySelector;
    private readonly Func<IGrouping<TKey, ListChange<TSource>>, TResult> _resultSelector;

    public GroupItemsBy(
        TOperator source,
        Func<TSource, TKey> keySelector,
        Func<IGrouping<TKey, ListChange<TSource>>, TResult> resultSelector,
        IEqualityComparer<TKey>? comparer)
    {
        _source = source.ThrowIfArgumentNull();
        _keyComparer = comparer;
        _keySelector = keySelector.ThrowIfArgumentNull();
        _resultSelector = resultSelector.ThrowIfArgumentNull();
    }

    public GroupItemsBy(
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
        if (_keySelector is Func<TSource, TKey> staticKeySelector)
        {
            return _source.Build<
                TBox,
                TBoxFactory,
                GroupItemsByStateMachine<
                    TransformItemStateMachine<
                        TContinuation,
                        FuncTransform<IGrouping<TKey, ListChange<TSource>>, TResult>,
                        IGrouping<TKey, ListChange<TSource>>,
                        TResult>,
                    GroupItemsByStaticState,
                    GroupItemsByStaticState.Manager<TSource, TKey>,
                    TSource,
                    TKey>>(
                        boxFactory, new(new(continuation, new(_resultSelector)), new(staticKeySelector), _keyComparer)
                    );
        }

        if (_keySelector is Func<TSource, IObservable<TKey>> dynamicKeySelector)
        {
            return _source.Build<
                TBox,
                TBoxFactory,
                GroupItemsByStateMachine<
                    TransformItemStateMachine<
                        TContinuation,
                        FuncTransform<IGrouping<TKey, ListChange<TSource>>, TResult>,
                        IGrouping<TKey, ListChange<TSource>>,
                        TResult>,
                    GroupItemsByDynamicState<TSource, TKey>,
                    GroupItemsByDynamicState<TSource, TKey>.Manager,
                    TSource,
                    TKey>>(
                        boxFactory, new(new(continuation, new(_resultSelector)), new(dynamicKeySelector), _keyComparer)
                    );
        }

        throw new NotSupportedException();
    }
}