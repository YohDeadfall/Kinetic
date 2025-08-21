using System;
using System.Runtime.InteropServices;
using Kinetic.Runtime;

namespace Kinetic.Linq;

[StructLayout(LayoutKind.Auto)]
public readonly struct Aggregate<TOperator, TSource> : IOperator<TSource>
    where TOperator : IOperator<TSource>
{
    private readonly TOperator _source;
    private readonly Func<TSource, TSource, TSource> _accumulator;

    public Aggregate(TOperator source, Func<TSource, TSource, TSource> accumulator)
    {
        _source = source.ThrowIfArgumentNull();
        _accumulator = accumulator.ThrowIfArgumentNull();
    }

    public TBox Build<TBox, TBoxFactory, TContinuation>(in TBoxFactory boxFactory, TContinuation continuation)
        where TBoxFactory : struct, IStateMachineBoxFactory<TBox>
        where TContinuation : struct, IStateMachine<TSource>
    {

        return _source.Build<TBox, TBoxFactory, AggregateStateMachine<TContinuation, Accumulator<FuncAggregator<TSource, TSource>, TSource>, TSource, TSource>>(
            boxFactory, new(continuation, new(aggregator: new(_accumulator))));
    }
}