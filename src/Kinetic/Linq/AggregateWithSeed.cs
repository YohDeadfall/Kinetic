using System;
using Kinetic.Runtime;

namespace Kinetic.Linq;

public readonly struct AggregateWithSeed<TOperator, TSource, TResult> : IOperator<TResult>
    where TOperator : IOperator<TSource>
{
    private readonly TOperator _source;
    private readonly TResult _seed;
    private readonly Func<TResult, TSource, TResult> _accumulator;

    public AggregateWithSeed(TOperator source, TResult seed, Func<TResult, TSource, TResult> accumulator)
    {
        _source = source.ThrowIfNull();
        _seed = seed;
        _accumulator = accumulator.ThrowIfNull();
    }

    public TBox Build<TBox, TBoxFactory, TContinuation>(in TBoxFactory boxFactory, TContinuation continuation)
        where TBoxFactory : struct, IStateMachineBoxFactory<TBox>
        where TContinuation : struct, IStateMachine<TResult>
    {

        return _source.Build<TBox, TBoxFactory, AggregateStateMachine<TContinuation, AccumulatorWithDefault<FuncAggregator<TSource, TResult>, TSource, TResult>, TSource, TResult>>(
            boxFactory, new(continuation, new(aggregator: new(_accumulator), _seed)));
    }
}