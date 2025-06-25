using System;
using Kinetic.Runtime;

namespace Kinetic.Linq;

public readonly struct WhereItem<TOperator, TSource> : IOperator<ListChange<TSource>>
    where TOperator : IOperator<ListChange<TSource>>
{
    private readonly TOperator _source;
    private readonly Func<TSource, bool> _predicate;

    public WhereItem(TOperator source, Func<TSource, bool> predicate)
    {
        _source = source.ThrowIfArgumentNull();
        _predicate = predicate.ThrowIfArgumentNull();
    }

    public TBox Build<TBox, TBoxFactory, TContinuation>(in TBoxFactory boxFactory, TContinuation continuation)
        where TBoxFactory : struct, IStateMachineBoxFactory<TBox>
        where TContinuation : struct, IStateMachine<ListChange<TSource>>
    {
        return _source.Build<TBox, TBoxFactory, FilterItemStateMachine<TContinuation, FuncTransform<TSource, bool>, TSource>>(
            boxFactory, new(continuation, new(_predicate)));
    }
}