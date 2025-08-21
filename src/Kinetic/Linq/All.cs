using System.Runtime.InteropServices;
using Kinetic.Runtime;

namespace Kinetic.Linq;

[StructLayout(LayoutKind.Auto)]
public readonly struct All<TOperator> : IOperator<bool>
    where TOperator : IOperator<bool>
{
    private readonly TOperator _source;

    public All(TOperator source) =>
        _source = source.ThrowIfArgumentNull();

    public TBox Build<TBox, TBoxFactory, TContinuation>(in TBoxFactory boxFactory, TContinuation continuation)
        where TBoxFactory : struct, IStateMachineBoxFactory<TBox>
        where TContinuation : struct, IStateMachine<bool>
    {
        return _source.Build<TBox, TBoxFactory, AllStateMachine<TContinuation>>(
            boxFactory, new(continuation));
    }
}