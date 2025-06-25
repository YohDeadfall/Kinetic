using System;
using System.Threading.Tasks;
using Kinetic.Runtime;

namespace Kinetic.Linq;

public readonly struct SelectAwaitTaskIndexed<TOperator, TSource, TResult> : IOperator<TResult>
    where TOperator : IOperator<TSource>
{
    private readonly TOperator _source;
    private readonly Func<TSource, int, Task<TResult>> _selector;

    public SelectAwaitTaskIndexed(TOperator source, Func<TSource, int, Task<TResult>> selector)
    {
        _source = source.ThrowIfArgumentNull();
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
                AwaiterForTaskFactory<TSource, TResult, FuncIndexedTransform<TSource, Task<TResult>>>,
                AwaiterForTask<TResult>, TSource, TResult>>(
            boxFactory, new(continuation, new(new(_selector))));
    }
}