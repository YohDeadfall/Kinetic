using System.Collections.Generic;
using System.Runtime.InteropServices;
using Kinetic.Runtime;

namespace Kinetic.Linq;

[StructLayout(LayoutKind.Auto)]
public readonly struct Contains<TOperator, TSource> : IOperator<bool>
    where TOperator : IOperator<TSource>
{
    private readonly TOperator _source;
    private readonly TSource _value;
    private readonly IEqualityComparer<TSource>? _comparer;

    public Contains(TOperator source, TSource value, IEqualityComparer<TSource>? comparer)
    {
        _source = source.ThrowIfArgumentNull();
        _value = value;
        _comparer = comparer;
    }

    public TBox Build<TBox, TBoxFactory, TContinuation>(in TBoxFactory boxFactory, TContinuation continuation)
        where TBoxFactory : struct, IStateMachineBoxFactory<TBox>
        where TContinuation : struct, IStateMachine<bool>
    {
        return _source.Build<TBox, TBoxFactory, AggregateStateMachine<TContinuation, AccumulatorWithDefault<ContainsAggregator<TSource>, TSource, bool>, TSource, bool>>(
            boxFactory, new(continuation, new(aggregator: new(_value, _comparer), defaultValue: false)));
    }
}