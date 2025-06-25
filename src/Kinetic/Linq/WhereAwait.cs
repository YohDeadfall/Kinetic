using System;
using System.Threading.Tasks;
using Kinetic.Runtime;

namespace Kinetic.Linq;

public readonly struct WhereAwait<TOperator, TSource> : IOperator<TSource>
    where TOperator : IOperator<TSource>
{
    private readonly TOperator _source;
    private readonly Delegate _predicate;

    public WhereAwait(TOperator source, Func<TSource, Task<bool>> predicate)
    {
        _source = source.ThrowIfArgumentNull();
        _predicate = predicate.ThrowIfArgumentNull();
    }

    public WhereAwait(TOperator source, Func<TSource, ValueTask<bool>> predicate)
    {
        _source = source.ThrowIfArgumentNull();
        _predicate = predicate.ThrowIfArgumentNull();
    }

    public TBox Build<TBox, TBoxFactory, TContinuation>(in TBoxFactory boxFactory, TContinuation continuation)
        where TBoxFactory : struct, IStateMachineBoxFactory<TBox>
        where TContinuation : struct, IStateMachine<TSource>
    {
        if (_predicate is Func<TSource, Task<bool>> taskPredicate)
            return _source.Build<
                TBox,
                TBoxFactory,
                FilterAwaitStateMachine<
                    TContinuation,
                    AwaiterForTaskFactory<TSource, bool>,
                    AwaiterForTask<bool>, TSource>>(
                boxFactory, new(continuation, new(taskPredicate)));

        if (_predicate is Func<TSource, ValueTask<bool>> valueTaskPredicate)
            return _source.Build<
                TBox,
                TBoxFactory,
                FilterAwaitStateMachine<
                    TContinuation,
                    AwaiterForValueTaskFactory<TSource, bool>,
                    AwaiterForValueTask<bool>, TSource>>(
                boxFactory, new(continuation, new(valueTaskPredicate)));

        throw new NotSupportedException();
    }
}