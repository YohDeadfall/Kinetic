using System;
using Kinetic.Runtime;

namespace Kinetic.Linq;

public readonly struct Subscribe<T> : IOperator<T>
{
    private readonly IObservable<T> _observable;

    public Subscribe(IObservable<T> source) =>
        _observable = source.ThrowIfNull();

    public TBox Build<TBox, TBoxFactory, TContinuation>(in TBoxFactory boxFactory, TContinuation continuation)
        where TBoxFactory : struct, IStateMachineBoxFactory<TBox>
        where TContinuation : struct, IStateMachine<T>
    {
        return boxFactory.Create<T, SubscribeStateMachine<TContinuation, T>>(
            new(continuation, _observable));
    }
}