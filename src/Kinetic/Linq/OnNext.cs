using System;
using Kinetic.Runtime;

namespace Kinetic.Linq;

public readonly struct OnNext<TOperator, TSource> : IOperator<TSource>
    where TOperator : IOperator<TSource>
{
    private readonly TOperator _source;
    private readonly Action<TSource> _onNext;

    public OnNext(TOperator source, Action<TSource> onNext)
    {
        _source = source.ThrowIfArgumentNull();
        _onNext = onNext.ThrowIfArgumentNull();
    }

    public TBox Build<TBox, TBoxFactory, TContinuation>(in TBoxFactory boxFactory, TContinuation continuation)
        where TBoxFactory : struct, IStateMachineBoxFactory<TBox>
        where TContinuation : struct, IStateMachine<TSource>
    {
        return _source.Build<TBox, TBoxFactory, OnNextStateMachine<TContinuation, TSource>>(
            boxFactory, new(continuation, _onNext));
    }
}