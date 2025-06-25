using Kinetic.Runtime;

namespace Kinetic.Linq;

public readonly struct First<TOperator, TSource> : IOperator<TSource>
    where TOperator : IOperator<TSource>
{
    private readonly TOperator _source;

    public First(TOperator source) =>
        _source = source.ThrowIfArgumentNull();

    public TBox Build<TBox, TBoxFactory, TContinuation>(in TBoxFactory boxFactory, TContinuation continuation)
        where TBoxFactory : struct, IStateMachineBoxFactory<TBox>
        where TContinuation : struct, IStateMachine<TSource>
    {
        return _source.Build<TBox, TBoxFactory, AggregateStateMachine<TContinuation, Accumulator<FirstAggregator<TSource>, TSource>, TSource, TSource>>(
            boxFactory, new(continuation, accumulator: new()));
    }
}