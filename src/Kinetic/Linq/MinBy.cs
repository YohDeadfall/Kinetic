using System;
using System.Collections.Generic;
using Kinetic.Runtime;

namespace Kinetic.Linq;

public readonly struct MinBy<TOperator, TSource, TKey> : IOperator<TSource>
    where TOperator : IOperator<TSource>
{
    private readonly TOperator _source;
    private readonly Func<TSource, TKey> _keySelector;
    private readonly IComparer<TKey>? _comparer;

    public MinBy(TOperator source, Func<TSource, TKey> keySelector, IComparer<TKey>? comparer)
    {
        _source = source.ThrowIfArgumentNull();
        _keySelector = keySelector.ThrowIfArgumentNull();
        _comparer = comparer;
    }

    public TBox Build<TBox, TBoxFactory, TContinuation>(in TBoxFactory boxFactory, TContinuation continuation)
        where TBoxFactory : struct, IStateMachineBoxFactory<TBox>
        where TContinuation : struct, IStateMachine<TSource>
    {
        return _source.Build<TBox, TBoxFactory, AggregateStateMachine<TContinuation, Accumulator<CompareBy<TSource, TKey, GreaterOrdering>, TSource>, TSource, TSource>>(
            boxFactory, new(continuation, new(aggregator: new(_keySelector, _comparer))));
    }
}