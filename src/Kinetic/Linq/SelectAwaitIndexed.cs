using System;
using System.Threading.Tasks;
using Kinetic.Runtime;

namespace Kinetic.Linq;

public readonly struct SelectAwaitIndexed<TOperator, TSource, TResult> : IOperator<TResult>
    where TOperator : IOperator<TSource>
{
    private readonly TOperator _source;
    private readonly Func<TSource, int, ValueTask<TResult>> _selector;

    public SelectAwaitIndexed(TOperator source, Func<TSource, int, ValueTask<TResult>> selector)
    {
        _source = source.ThrowIfNull();
        _selector = selector.ThrowIfNull();
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
                AwaiterForValueTaskFactory<TSource, TResult, FuncIndexedTransform<TSource, ValueTask<TResult>>>,
                AwaiterForValueTask<TResult>, TSource, TResult>>(
            boxFactory, new(continuation, new(new(_selector))));
    }
}