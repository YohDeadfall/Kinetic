using System;
using System.Runtime.InteropServices;
using Kinetic.Runtime;

namespace Kinetic.Linq;

[StructLayout(LayoutKind.Auto)]
public readonly struct Subscribe<T> : IOperator<T>
{
    private readonly IObservable<T> _observable;

    public Subscribe(IObservable<T> source) =>
        _observable = source.ThrowIfArgumentNull();

    public TBox Build<TBox, TBoxFactory, TContinuation>(in TBoxFactory boxFactory, TContinuation continuation)
        where TBoxFactory : struct, IStateMachineBoxFactory<TBox>
        where TContinuation : struct, IStateMachine<T>
    {
        return boxFactory.Create<T, SubscribeStateMachine<TContinuation, T>>(
            new(continuation, _observable));
    }
}