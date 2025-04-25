using Kinetic.Runtime;

namespace Kinetic.Linq;

internal struct AccumulatorWithDefault<TAggregator, TSource, TResult> : IAccumulator<TSource, TResult>
    where TAggregator : struct, IAggregator<TSource, TResult>
{
    private TAggregator _aggregator;
    private TResult _result;

    public AccumulatorWithDefault(TAggregator aggregator, TResult defaultValue)
    {
        _aggregator = aggregator;
        _result = defaultValue;
    }

    public bool Accumulate(TSource value) =>
        _aggregator.Aggregate(value, ref _result);

    public void Publish<TStateMachine>(ref TStateMachine stateMachine)
        where TStateMachine : struct, IStateMachine<TResult>
    {
        stateMachine.OnNext(_result);
    }
}
