using System.Runtime.InteropServices;
using Kinetic.Runtime;

namespace Kinetic.Linq;

[StructLayout(LayoutKind.Auto)]
public readonly struct Skip<TOperator, TSource> : IOperator<TSource>
    where TOperator : IOperator<TSource>
{
    private readonly TOperator _source;
    private readonly int _count;

    public Skip(TOperator source, int count)
    {
        _source = source.ThrowIfArgumentNull();
        _count = count.ThrowIfArgumentNegative();
    }

    public TBox Build<TBox, TBoxFactory, TContinuation>(in TBoxFactory boxFactory, TContinuation continuation)
        where TBoxFactory : struct, IStateMachineBoxFactory<TBox>
        where TContinuation : struct, IStateMachine<TSource>
    {
        return _source.Build<TBox, TBoxFactory, FilterStateMachine<TContinuation, SkipFilter<TSource>, TSource>>(
            boxFactory, new(continuation, new(_count)));
    }
}