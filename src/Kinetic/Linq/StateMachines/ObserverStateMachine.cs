using System;

namespace Kinetic.Linq.StateMachines;

internal struct ObserverStateMachine<T, TContinuation> : IStateMachine<T>
    where TContinuation : struct, IStateMachine<T>
{
    private TContinuation _continuation;
    private IObservable<T>? _observable;
    private IDisposable? _subscription;

    public ObserverStateMachine(in TContinuation continuation, IObservable<T> observable)
    {
        _continuation = continuation;
        _observable = observable;
        _subscription = null;
    }

    public StateMachineBox Box =>
        _continuation.Box;

    public StateMachine<T> Reference =>
        StateMachine<T>.Create(ref this);

    public StateMachine? Continuation =>
        _continuation.Reference;

    public void Initialize(StateMachineBox box)
    {
        _continuation.Initialize(box);
        _subscription = _observable?.Subscribe(
            (StateMachineBox<T, ObserverStateMachine<T, TContinuation>>) box);
    }

    public void Dispose()
    {
        _subscription?.Dispose();
        _continuation.Dispose();
    }

    public void OnCompleted() =>
        _continuation.OnCompleted();

    public void OnError(Exception error) =>
        _continuation.OnError(error);

    public void OnNext(T value) =>
        _continuation.OnNext(value);
}