using System;
using Kinetic.Runtime;

namespace Kinetic.Linq;

public readonly struct SelectObservableItems<TOperator, TSource, TResult> : IOperator<ListChange<TResult>>
    where TOperator : IOperator<ListChange<TSource>>
{
    private readonly TOperator _source;
    private readonly Func<TSource, IObservable<TResult>> _selector;

    public SelectObservableItems(TOperator source, Func<TSource, IObservable<TResult>> selector)
    {
        _source = source.ThrowIfArgumentNull();
        _selector = selector.ThrowIfArgumentNull();
    }

    public TBox Build<TBox, TBoxFactory, TContinuation>(in TBoxFactory boxFactory, TContinuation continuation)
        where TBoxFactory : struct, IStateMachineBoxFactory<TBox>
        where TContinuation : struct, IStateMachine<ListChange<TResult>>
    {
        return _source.Build<TBox, TBoxFactory, TransformObservableItemsStateMachine<TContinuation, TSource, TResult>>(
            boxFactory, new(continuation, _selector));
    }
}