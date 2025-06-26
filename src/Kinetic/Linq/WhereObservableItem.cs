using System;
using Kinetic.Runtime;

namespace Kinetic.Linq;

public readonly struct WhereObservableItem<TOperator, TSource> : IOperator<ListChange<TSource>>
    where TOperator : IOperator<ListChange<TSource>>
{
    private readonly TOperator _source;
    private readonly Func<TSource, IObservable<bool>> _predicate;

    public WhereObservableItem(TOperator source, Func<TSource, IObservable<bool>> predicate)
    {
        _source = source.ThrowIfArgumentNull();
        _predicate = predicate.ThrowIfArgumentNull();
    }

    public TBox Build<TBox, TBoxFactory, TContinuation>(in TBoxFactory boxFactory, TContinuation continuation)
        where TBoxFactory : struct, IStateMachineBoxFactory<TBox>
        where TContinuation : struct, IStateMachine<ListChange<TSource>>
    {
        return _source.Build<TBox, TBoxFactory, FilterObservableItemsStateMachine<TContinuation, TSource>>(
            boxFactory, new(continuation, _predicate));
    }
}