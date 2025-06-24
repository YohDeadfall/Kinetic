using System;
using Kinetic.Runtime;

namespace Kinetic.Linq;

internal struct SubscribeStateMachine<TContinuation, T> : IStateMachine<T>
    where TContinuation : struct, IStateMachine<T>
{
    private TContinuation _continuation;
    private IDisposable? _subscription;
    private readonly IObservable<T> _observable;

    public SubscribeStateMachine(TContinuation continuation, IObservable<T> observable)
    {
        _continuation = continuation;
        _observable = observable;
    }

    public StateMachineBox Box =>
        _continuation.Box;

    public StateMachineReference<T> Reference =>
        StateMachineReference<T>.Create(ref this);

    public StateMachineReference? Continuation =>
        _continuation.Reference;

    public void Dispose()
    {
        _subscription?.Dispose();
        _continuation.Dispose();
    }

    public void Initialize(StateMachineBox box)
    {
        _continuation.Initialize(box);
        _subscription = _observable.Subscribe(box as IObserver<T> ?? Reference);
    }

    public void OnCompleted() =>
        _continuation.OnCompleted();

    public void OnError(Exception error) =>
        _continuation.OnError(error);

    public void OnNext(T value) =>
        _continuation.OnNext(value);
}