using System;
using System.Runtime.InteropServices;
using Kinetic.Runtime;

namespace Kinetic.Linq;

[StructLayout(LayoutKind.Auto)]
public readonly struct SelectItems<TOperator, TSource, TResult> : IOperator<ListChange<TResult>>
    where TOperator : IOperator<ListChange<TSource>>
{
    private readonly TOperator _source;
    private readonly Func<TSource, TResult> _selector;

    public SelectItems(TOperator source, Func<TSource, TResult> selector)
    {
        _source = source.ThrowIfArgumentNull();
        _selector = selector.ThrowIfArgumentNull();
    }

    public TBox Build<TBox, TBoxFactory, TContinuation>(in TBoxFactory boxFactory, TContinuation continuation)
        where TBoxFactory : struct, IStateMachineBoxFactory<TBox>
        where TContinuation : struct, IStateMachine<ListChange<TResult>>
    {
        return _source.Build<TBox, TBoxFactory, TransformItemsStateMachine<TContinuation, FuncTransform<TSource, TResult>, TSource, TResult>>(
            boxFactory, new(continuation, new(_selector)));
    }
}