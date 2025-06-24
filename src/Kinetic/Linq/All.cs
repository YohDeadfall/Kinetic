using Kinetic.Runtime;

namespace Kinetic.Linq;

public readonly struct All<TOperator> : IOperator<bool>
    where TOperator : IOperator<bool>
{
    private readonly TOperator _source;

    public All(TOperator source) =>
        _source = source.ThrowIfNull();

    public TBox Build<TBox, TBoxFactory, TContinuation>(in TBoxFactory boxFactory, TContinuation continuation)
        where TBoxFactory : struct, IStateMachineBoxFactory<TBox>
        where TContinuation : struct, IStateMachine<bool>
    {
        return _source.Build<TBox, TBoxFactory, AllStateMachine<TContinuation>>(
            boxFactory, new(continuation));
    }
}