using System;
using Kinetic.Runtime;

namespace Kinetic.Linq;

public readonly struct TakeWhileIndexed<TOperator, TSource> : IOperator<TSource>
    where TOperator : IOperator<TSource>
{
    private readonly TOperator _source;
    private readonly Func<TSource, int, bool> _predicate;

    public TakeWhileIndexed(TOperator source, Func<TSource, int, bool> predicate)
    {
        _source = source.ThrowIfNull();
        _predicate = predicate.ThrowIfNull();
    }

    public TBox Build<TBox, TBoxFactory, TContinuation>(in TBoxFactory boxFactory, TContinuation continuation)
        where TBoxFactory : struct, IStateMachineBoxFactory<TBox>
        where TContinuation : struct, IStateMachine<TSource>
    {
        return _source.Build<TBox, TBoxFactory, FilterStateMachine<TContinuation, TakeWhileIndexedFilter<TSource>, TSource>>(
            boxFactory, new(continuation, new(_predicate)));
    }
}