using System;
using System.Diagnostics.CodeAnalysis;
using Kinetic.Runtime;

namespace Kinetic.Linq;

internal struct Accumulator<TAggregator, TSource> : IAccumulator<TSource, TSource>
    where TAggregator : struct, IAggregator<TSource, TSource>
{
    private TAggregator _aggregator;

    [AllowNull]
    private TSource _result;
    private bool _hasResult;

    public Accumulator(TAggregator aggregator) =>
        _aggregator = aggregator;

    public bool Accumulate(TSource value)
    {
        if (TAggregator.RequiresSeed)
        {
            if (_hasResult)
                return _aggregator.Aggregate(value, ref _result);

            _hasResult = true;
            _result = value;

            return true;
        }
        else
        {
            _hasResult = true;

            return _aggregator.Aggregate(value, ref _result);
        }
    }

    public void Publish<TStateMachine>(ref TStateMachine stateMachine)
        where TStateMachine : struct, IStateMachine<TSource>
    {
        if (_hasResult)
            stateMachine.OnNext(_result);
        else
            throw new InvalidOperationException("No values were accumulated.");
    }
}
