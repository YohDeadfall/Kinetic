using System.Collections.Generic;
using System.Runtime.InteropServices;
using Kinetic.Runtime;

namespace Kinetic.Linq;

[StructLayout(LayoutKind.Auto)]
public readonly struct ToArray<TOperator, TSource> : IOperator<TSource[]>
    where TOperator : IOperator<TSource>
{
    private readonly TOperator _source;

    public ToArray(TOperator source) =>
        _source = source.ThrowIfArgumentNull();

    public TBox Build<TBox, TBoxFactory, TContinuation>(in TBoxFactory boxFactory, TContinuation continuation)
        where TBoxFactory : struct, IStateMachineBoxFactory<TBox>
        where TContinuation : struct, IStateMachine<TSource[]>
    {
        return _source.Build<
            TBox,
            TBoxFactory,
            AggregateStateMachine<
                TransformStateMachine<
                    TContinuation,
                    FuncTransform<List<TSource>, TSource[]>,
                    List<TSource>,
                    TSource[]>,
                CollectIntoAccumulator<TSource, List<TSource>>,
                TSource,
                List<TSource>>>(
            boxFactory, new(new(continuation, new(list => list.ToArray())), accumulator: new(new())));
    }
}