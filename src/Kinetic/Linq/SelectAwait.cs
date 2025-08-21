using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Kinetic.Runtime;

namespace Kinetic.Linq;

[StructLayout(LayoutKind.Auto)]
public readonly struct SelectAwait<TOperator, TSource, TResult> : IOperator<TResult>
    where TOperator : IOperator<TSource>
{
    private readonly TOperator _source;
    private readonly Func<TSource, ValueTask<TResult>> _selector;

    public SelectAwait(TOperator source, Func<TSource, ValueTask<TResult>> selector)
    {
        _source = source;
        _selector = selector.ThrowIfArgumentNull();
    }

    public TBox Build<TBox, TBoxFactory, TContinuation>(in TBoxFactory boxFactory, TContinuation continuation)
        where TBoxFactory : struct, IStateMachineBoxFactory<TBox>
        where TContinuation : struct, IStateMachine<TResult>
    {
        return _source.Build<
            TBox,
            TBoxFactory,
            TransformAwaitStateMachine<
                TContinuation,
                AwaiterForValueTaskFactory<TSource, TResult>,
                AwaiterForValueTask<TResult>, TSource, TResult>>(
            boxFactory, new(continuation, new(_selector)));
    }
}