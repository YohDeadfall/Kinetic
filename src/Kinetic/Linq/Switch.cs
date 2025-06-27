using System;
using Kinetic.Runtime;

namespace Kinetic.Linq;

public readonly struct Switch<TOperator, TSource> : IOperator<TSource>
    where TOperator : IOperator<IObservable<TSource>?>
{
    private readonly TOperator _source;

    public Switch(TOperator source) =>
        _source = source.ThrowIfArgumentNull();

    public TBox Build<TBox, TBoxFactory, TContinuation>(in TBoxFactory boxFactory, TContinuation continuation)
        where TBoxFactory : struct, IStateMachineBoxFactory<TBox>
        where TContinuation : struct, IStateMachine<TSource>
    {
        throw new NotImplementedException();
    }
}