using System.Runtime.InteropServices;
using Kinetic.Runtime;

namespace Kinetic.Linq;

[StructLayout(LayoutKind.Auto)]
public readonly struct Cast<TOperator, TSource, TResult> : IOperator<TResult>
    where TOperator : IOperator<TSource>
{
    private readonly TOperator _source;

    public Cast(TOperator source) =>
        _source = source.ThrowIfArgumentNull();

    public TBox Build<TBox, TBoxFactory, TContinuation>(in TBoxFactory boxFactory, TContinuation continuation)
        where TBoxFactory : struct, IStateMachineBoxFactory<TBox>
        where TContinuation : struct, IStateMachine<TResult>
    {
        return _source.Build<TBox, TBoxFactory, CastStateMachine<TContinuation, TSource, TResult>>(
            boxFactory, new(continuation));
    }
}