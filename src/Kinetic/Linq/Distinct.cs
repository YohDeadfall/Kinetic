using System.Collections.Generic;
using Kinetic.Runtime;

namespace Kinetic.Linq;

public readonly struct Distinct<TOperator, TSource> : IOperator<TSource>
    where TOperator : IOperator<TSource>
{
    private readonly TOperator _source;
    private readonly IEqualityComparer<TSource>? _comparer;

    public Distinct(TOperator source, IEqualityComparer<TSource>? comparer)
    {
        _source = source.ThrowIfNull();
        _comparer = comparer;
    }

    public TBox Build<TBox, TBoxFactory, TContinuation>(in TBoxFactory boxFactory, TContinuation continuation)
        where TBoxFactory : struct, IStateMachineBoxFactory<TBox>
        where TContinuation : struct, IStateMachine<TSource>
    {
        return _source.Build<TBox, TBoxFactory, FilterStateMachine<TContinuation, DistinctFilter<TSource>, TSource>>(
            boxFactory, new(continuation, new(_comparer)));
    }
}

