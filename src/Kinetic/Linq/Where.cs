using System;
using Kinetic.Runtime;

namespace Kinetic.Linq;

public readonly struct Where<TOperator, TSource> : IOperator<TSource>
    where TOperator : IOperator<TSource>
{
    private readonly TOperator _source;
    private readonly Func<TSource, bool> _predicate;

    public Where(TOperator source, Func<TSource, bool> predicate)
    {
        _source = source.ThrowIfArgumentNull();
        _predicate = predicate.ThrowIfArgumentNull();
    }

    public TBox Build<TBox, TBoxFactory, TContinuation>(in TBoxFactory boxFactory, TContinuation continuation)
        where TBoxFactory : struct, IStateMachineBoxFactory<TBox>
        where TContinuation : struct, IStateMachine<TSource>
    {
        return _source.Build<TBox, TBoxFactory, FilterStateMachine<TContinuation, FuncTransform<TSource, bool>, TSource>>(
            boxFactory, new(continuation, new(_predicate)));
    }
}