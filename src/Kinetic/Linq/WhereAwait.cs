using System;
using System.Threading.Tasks;
using Kinetic.Runtime;

namespace Kinetic.Linq;

public readonly struct WhereAwait<TOperator, TSource> : IOperator<TSource>
    where TOperator : IOperator<TSource>
{
    private readonly TOperator _source;
    private readonly Func<TSource, ValueTask<bool>> _predicate;

    public WhereAwait(TOperator source, Func<TSource, ValueTask<bool>> predicate)
    {
        _source = source.ThrowIfArgumentNull();
        _predicate = predicate.ThrowIfArgumentNull();
    }

    public TBox Build<TBox, TBoxFactory, TContinuation>(in TBoxFactory boxFactory, TContinuation continuation)
        where TBoxFactory : struct, IStateMachineBoxFactory<TBox>
        where TContinuation : struct, IStateMachine<TSource>
    {
        return _source.Build<TBox, TBoxFactory, FilterAwaitStateMachine<TContinuation, AwaiterForValueTaskFactory<TSource, bool>, AwaiterForValueTask<bool>, TSource>>(
            boxFactory, new(continuation, new(_predicate)));
    }
}