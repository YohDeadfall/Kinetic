using System.Runtime.InteropServices;
using Kinetic.Runtime;

namespace Kinetic.Linq;

[StructLayout(LayoutKind.Auto)]
public readonly struct Count<TOperator, TSource> : IOperator<int>
    where TOperator : IOperator<TSource>
{
    private readonly TOperator _source;

    public Count(TOperator source) =>
        _source = source.ThrowIfArgumentNull();

    public TBox Build<TBox, TBoxFactory, TContinuation>(in TBoxFactory boxFactory, TContinuation continuation)
        where TBoxFactory : struct, IStateMachineBoxFactory<TBox>
        where TContinuation : struct, IStateMachine<int>
    {
        return _source.Build<TBox, TBoxFactory, AggregateStateMachine<TContinuation, CountAccumulator<TSource>, TSource, int>>(
            boxFactory, new(continuation, new()));
    }
}