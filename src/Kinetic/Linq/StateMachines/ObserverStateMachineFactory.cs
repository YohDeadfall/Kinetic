using System;

namespace Kinetic.Linq.StateMachines;

internal readonly struct ObserverStateMachineFactory<T> : IStateMachineFactory<T, T>
{
    private readonly IObservable<T> _observable;

    public ObserverStateMachineFactory(IObservable<T> observable) =>
        _observable = observable;

    public void Create<TContinuation>(in TContinuation continuation, StateMachines.ObserverStateMachine<T> source)
        where TContinuation : struct, IStateMachine<T> =>
        source.ContinueWith(new ObserverStateMachine<T, TContinuation>(continuation, _observable));
}