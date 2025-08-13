using Kinetic.Runtime;

namespace Kinetic.Linq;

public readonly struct Any<TOperator, TSource> : IOperator<bool>
    where TOperator : IOperator<TSource>
{
    private readonly TOperator _source;

    public Any(TOperator source) =>
        _source = source.ThrowIfArgumentNull();

    public TBox Build<TBox, TBoxFactory, TContinuation>(in TBoxFactory boxFactory, TContinuation continuation)
        where TBoxFactory : struct, IStateMachineBoxFactory<TBox>
        where TContinuation : struct, IStateMachine<bool>
    {
        return _source.Build<TBox, TBoxFactory, AnyStateMachine<TContinuation, TSource>>(
            boxFactory, new(continuation));
    }
}