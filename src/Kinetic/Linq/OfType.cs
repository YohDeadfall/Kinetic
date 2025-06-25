using Kinetic.Runtime;

namespace Kinetic.Linq;

public readonly struct OfType<TOperator, TSource, TResult> : IOperator<TResult>
    where TOperator : IOperator<TSource>
{
    private readonly TOperator _source;

    public OfType(TOperator source) =>
        _source = source.ThrowIfArgumentNull();

    public TBox Build<TBox, TBoxFactory, TContinuation>(in TBoxFactory boxFactory, TContinuation continuation)
        where TBoxFactory : struct, IStateMachineBoxFactory<TBox>
        where TContinuation : struct, IStateMachine<TResult>
    {
        return _source.Build<TBox, TBoxFactory, OfTypeStateMachine<TContinuation, TSource, TResult>>(
            boxFactory, new(continuation));
    }
}