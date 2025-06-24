using Kinetic.Runtime;

namespace Kinetic.Linq;

public readonly struct FirstOrDefault<TOperator, TSource> : IOperator<TSource>
    where TOperator : IOperator<TSource>
{
    private readonly TOperator _source;
    private readonly TSource _defaultValue;

    public FirstOrDefault(TOperator source, TSource defaultValue)
    {
        _source = source.ThrowIfNull();
        _defaultValue = defaultValue;
    }

    public TBox Build<TBox, TBoxFactory, TContinuation>(in TBoxFactory boxFactory, TContinuation continuation)
        where TBoxFactory : struct, IStateMachineBoxFactory<TBox>
        where TContinuation : struct, IStateMachine<TSource>
    {
        return _source.Build<TBox, TBoxFactory, AggregateStateMachine<TContinuation, AccumulatorWithDefault<FirstAggregator<TSource>, TSource, TSource>, TSource, TSource>>(
            boxFactory, new(continuation, new(aggregator: new(), _defaultValue)));
    }
}