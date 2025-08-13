using System;
using Kinetic.Runtime;

namespace Kinetic.Linq;

public readonly struct OnCompleted<TOperator, TSource> : IOperator<TSource>
    where TOperator : IOperator<TSource>
{
    private readonly TOperator _source;
    private readonly Action _onCompleted;

    public OnCompleted(TOperator source, Action onCompleted)
    {
        _source = source.ThrowIfArgumentNull();
        _onCompleted = onCompleted.ThrowIfArgumentNull();
    }

    public TBox Build<TBox, TBoxFactory, TContinuation>(in TBoxFactory boxFactory, TContinuation continuation)
        where TBoxFactory : struct, IStateMachineBoxFactory<TBox>
        where TContinuation : struct, IStateMachine<TSource>
    {
        return _source.Build<TBox, TBoxFactory, OnCompletedStateMachine<TContinuation, TSource>>(
            boxFactory, new(continuation, _onCompleted));
    }
}