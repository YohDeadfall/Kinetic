using Kinetic.Runtime;

namespace Kinetic.Linq;

public readonly struct SwitchToReadOnlyProperty<TOperator, TSource> : IOperator<TSource>
    where TOperator : IOperator<ReadOnlyProperty<TSource>?>
{
    private readonly TOperator _source;

    public SwitchToReadOnlyProperty(TOperator source) =>
        _source = source.ThrowIfArgumentNull();

    public TBox Build<TBox, TBoxFactory, TContinuation>(in TBoxFactory boxFactory, TContinuation continuation)
        where TBoxFactory : struct, IStateMachineBoxFactory<TBox>
        where TContinuation : struct, IStateMachine<TSource>
    {
        return _source.Build<TBox, TBoxFactory, SwitchToPropertyStateMachine<TContinuation, TSource>>(
            boxFactory, new(continuation));
    }
}