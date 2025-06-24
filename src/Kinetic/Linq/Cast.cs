using Kinetic.Runtime;

namespace Kinetic.Linq;

public readonly struct Cast<TOperator, TSource, TResult> : IOperator<TResult>
    where TOperator : IOperator<TSource>
{
    private readonly TOperator _source;

    public Cast(TOperator source) =>
        _source = source.ThrowIfNull();

    public TBox Build<TBox, TBoxFactory, TContinuation>(in TBoxFactory boxFactory, TContinuation continuation)
        where TBoxFactory : struct, IStateMachineBoxFactory<TBox>
        where TContinuation : struct, IStateMachine<TResult>
    {
        return _source.Build<TBox, TBoxFactory, CastStateMachine<TContinuation, TSource, TResult>>(
            boxFactory, new(continuation));
    }
}