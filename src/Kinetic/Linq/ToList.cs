using System.Collections.Generic;
using Kinetic.Runtime;

namespace Kinetic.Linq;

public readonly struct ToList<TOperator, TSource> : IOperator<List<TSource>>
    where TOperator : IOperator<TSource>
{
    private readonly TOperator _source;

    public ToList(TOperator source) =>
        _source = source.ThrowIfArgumentNull();

    public TBox Build<TBox, TBoxFactory, TContinuation>(in TBoxFactory boxFactory, TContinuation continuation)
        where TBoxFactory : struct, IStateMachineBoxFactory<TBox>
        where TContinuation : struct, IStateMachine<List<TSource>>
    {
        return _source.Build<
            TBox,
            TBoxFactory,
            AggregateStateMachine<
                TContinuation,
                CollectIntoAccumulator<TSource, List<TSource>>,
                TSource,
                List<TSource>>>(
            boxFactory, new(continuation, accumulator: new(new())));
    }
}