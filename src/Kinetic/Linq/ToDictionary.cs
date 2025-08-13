using System.Collections.Generic;
using Kinetic.Runtime;

namespace Kinetic.Linq;

public readonly struct ToDictionary<TOperator, TSource, TKey> : IOperator<Dictionary<TKey, TSource>>
    where TOperator : IOperator<KeyValuePair<TKey, TSource>>
    where TKey : notnull
{
    private readonly TOperator _source;
    private readonly IEqualityComparer<TKey>? _comparer;

    public ToDictionary(TOperator source, IEqualityComparer<TKey>? comparer)
    {
        _source = source.ThrowIfArgumentNull();
        _comparer = comparer;
    }

    public TBox Build<TBox, TBoxFactory, TContinuation>(in TBoxFactory boxFactory, TContinuation continuation)
        where TBoxFactory : struct, IStateMachineBoxFactory<TBox>
        where TContinuation : struct, IStateMachine<Dictionary<TKey, TSource>>
    {
        return _source.Build<
            TBox,
            TBoxFactory,
            AggregateStateMachine<
                TContinuation,
                CollectIntoAccumulator<KeyValuePair<TKey, TSource>, Dictionary<TKey, TSource>>,
                KeyValuePair<TKey, TSource>,
                Dictionary<TKey, TSource>>>(
            boxFactory, new(continuation, accumulator: new(new(_comparer))));
    }
}