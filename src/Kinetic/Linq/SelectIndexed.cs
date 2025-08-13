using System;
using Kinetic.Runtime;

namespace Kinetic.Linq;

public readonly struct SelectIndexed<TOperator, TSource, TResult> : IOperator<TResult>
    where TOperator : IOperator<TSource>
{
    private readonly TOperator _source;
    private readonly Func<TSource, int, TResult> _selector;

    public SelectIndexed(TOperator source, Func<TSource, int, TResult> selector)
    {
        _source = source.ThrowIfArgumentNull();
        _selector = selector.ThrowIfArgumentNull();
    }

    public TBox Build<TBox, TBoxFactory, TContinuation>(in TBoxFactory boxFactory, TContinuation continuation)
        where TBoxFactory : struct, IStateMachineBoxFactory<TBox>
        where TContinuation : struct, IStateMachine<TResult>
    {
        return _source.Build<TBox, TBoxFactory, TransformStateMachine<TContinuation, FuncIndexedTransform<TSource, TResult>, TSource, TResult>>(
            boxFactory, new(continuation, new(_selector)));
    }
}