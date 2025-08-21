using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Kinetic.Runtime;

namespace Kinetic.Linq;

[StructLayout(LayoutKind.Auto)]
public readonly struct DistinctBy<TOperator, TSource, TKey> : IOperator<TSource>
    where TOperator : IOperator<TSource>
{
    private readonly TOperator _source;
    private readonly Func<TSource, TKey> _keySelector;
    private readonly IEqualityComparer<TKey>? _comparer;

    public DistinctBy(TOperator source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey>? comparer)
    {
        _source = source.ThrowIfArgumentNull();
        _keySelector = keySelector.ThrowIfArgumentNull();
        _comparer = comparer;
    }

    public TBox Build<TBox, TBoxFactory, TContinuation>(in TBoxFactory boxFactory, TContinuation continuation)
        where TBoxFactory : struct, IStateMachineBoxFactory<TBox>
        where TContinuation : struct, IStateMachine<TSource>
    {
        return _source.Build<TBox, TBoxFactory, FilterStateMachine<TContinuation, DistinctByFilter<TSource, TKey>, TSource>>(
            boxFactory, new(continuation, new(_keySelector, _comparer)));
    }
}