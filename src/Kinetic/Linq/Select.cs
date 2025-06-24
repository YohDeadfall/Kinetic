using System;
using Kinetic.Runtime;

namespace Kinetic.Linq;

public readonly struct Select<TOperator, TSource, TResult> : IOperator<TResult>
    where TOperator : IOperator<TSource>
{
    private readonly TOperator _source;
    private readonly Func<TSource, TResult> _selector;

    public Select(TOperator source, Func<TSource, TResult> selector)
    {
        _source = source;
        _selector = selector.ThrowIfNull();
    }

    public TBox Build<TBox, TBoxFactory, TContinuation>(in TBoxFactory boxFactory, TContinuation continuation)
        where TBoxFactory : struct, IStateMachineBoxFactory<TBox>
        where TContinuation : struct, IStateMachine<TResult>
    {
        return _source.Build<TBox, TBoxFactory, TransformStateMachine<TContinuation, FuncTransform<TSource, TResult>, TSource, TResult>>(
            boxFactory, new(continuation, new(_selector)));
    }
}