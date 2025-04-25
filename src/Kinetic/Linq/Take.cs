using Kinetic.Runtime;

namespace Kinetic.Linq;

public readonly struct Take<TOperator, TSource> : IOperator<TSource>
    where TOperator : IOperator<TSource>
{
    private readonly TOperator _source;
    private readonly int _count;

    public Take(TOperator source, int count)
    {
        _source = source.ThrowIfNull();
        _count = count.ThrowIfNegative();
    }
    
    public TBox Build<TBox, TBoxFactory, TContinuation>(in TBoxFactory boxFactory, TContinuation continuation)
        where TBoxFactory : struct, IStateMachineBoxFactory<TBox>
        where TContinuation : struct, IStateMachine<TSource>
    {
        return _source.Build<TBox, TBoxFactory, FilterStateMachine<TContinuation, TakeFilter<TSource>, TSource>>(
            boxFactory, new(continuation, new(_count)));
    }
}
