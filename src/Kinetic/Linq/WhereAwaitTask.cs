using System;
using System.Threading.Tasks;
using Kinetic.Runtime;

namespace Kinetic.Linq;

public readonly struct WhereAwaitTask<TOperator, TSource> : IOperator<TSource>
    where TOperator : IOperator<TSource>
{
    private readonly TOperator _source;
    private readonly Func<TSource, Task<bool>> _predicate;

    public WhereAwaitTask(TOperator source, Func<TSource, Task<bool>> predicate)
    {
        _source = source.ThrowIfArgumentNull();
        _predicate = predicate.ThrowIfArgumentNull();
    }

    public TBox Build<TBox, TBoxFactory, TContinuation>(in TBoxFactory boxFactory, TContinuation continuation)
        where TBoxFactory : struct, IStateMachineBoxFactory<TBox>
        where TContinuation : struct, IStateMachine<TSource>
    {
        return _source.Build<
            TBox, TBoxFactory,
            FilterAwaitStateMachine<
                TContinuation,
                AwaiterForTaskFactory<TSource, bool>,
                AwaiterForTask<bool>, TSource>>(
            boxFactory, new(continuation, new(_predicate)));
    }
}