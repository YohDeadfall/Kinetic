using System;
using System.Runtime.InteropServices;
using Kinetic.Runtime;

namespace Kinetic.Linq;

[StructLayout(LayoutKind.Auto)]
public readonly struct OnItemAdded<TOperator, TSource> : IOperator<ListChange<TSource>>
    where TOperator : IOperator<ListChange<TSource>>
{
    private readonly TOperator _source;
    private readonly Action<TSource> _onAdded;

    public OnItemAdded(TOperator source, Action<TSource> onAdded)
    {
        _source = source.ThrowIfArgumentNull();
        _onAdded = onAdded.ThrowIfArgumentNull();
    }

    public TBox Build<TBox, TBoxFactory, TContinuation>(in TBoxFactory boxFactory, TContinuation continuation)
        where TBoxFactory : struct, IStateMachineBoxFactory<TBox>
        where TContinuation : struct, IStateMachine<ListChange<TSource>>
    {
        return _source.Build<TBox, TBoxFactory, OnItemAddedStateMachine<TContinuation, TSource>>(
            boxFactory, new(continuation, _onAdded));
    }
}