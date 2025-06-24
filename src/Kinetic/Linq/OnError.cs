using System;
using Kinetic.Runtime;

namespace Kinetic.Linq;

public readonly struct OnError<TOperator, TSource> : IOperator<TSource>
    where TOperator : IOperator<TSource>
{
    private readonly TOperator _source;
    private readonly Action<Exception> _onError;

    public OnError(TOperator source, Action<Exception> onError)
    {
        _source = source.ThrowIfNull();
        _onError = onError.ThrowIfNull();
    }

    public TBox Build<TBox, TBoxFactory, TContinuation>(in TBoxFactory boxFactory, TContinuation continuation)
        where TBoxFactory : struct, IStateMachineBoxFactory<TBox>
        where TContinuation : struct, IStateMachine<TSource>
    {
        return _source.Build<TBox, TBoxFactory, OnErrorStateMachine<TContinuation, TSource>>(
            boxFactory, new(continuation, _onError));
    }
}