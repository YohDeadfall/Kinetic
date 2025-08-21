using System.Collections.Generic;
using System.Runtime.InteropServices;
using Kinetic.Runtime;

namespace Kinetic.Linq;

[StructLayout(LayoutKind.Auto)]
public readonly struct Min<TOperator, TSource> : IOperator<TSource>
    where TOperator : IOperator<TSource>
{
    private readonly TOperator _source;
    private readonly IComparer<TSource>? _comparer;

    public Min(TOperator source, IComparer<TSource>? comparer)
    {
        _source = source.ThrowIfArgumentNull();
        _comparer = comparer;
    }

    public TBox Build<TBox, TBoxFactory, TContinuation>(in TBoxFactory boxFactory, TContinuation continuation)
        where TBoxFactory : struct, IStateMachineBoxFactory<TBox>
        where TContinuation : struct, IStateMachine<TSource>
    {
        return _source.Build<TBox, TBoxFactory, AggregateStateMachine<TContinuation, Accumulator<Compare<TSource, LessOrdering>, TSource>, TSource, TSource>>(
            boxFactory, new(continuation, new(aggregator: new(_comparer))));
    }
}