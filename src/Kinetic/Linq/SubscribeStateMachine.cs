using System;
using Kinetic.Runtime;

namespace Kinetic.Linq;

internal struct SubscribeStateMachine<TContinuation, T> : ISubscribeStateMachine<T>
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
        if (box is IObservable<T>)
        {
            if (_subscription is null)
            {
                _continuation.Initialize(box);
                _subscription = Subscription.Cold;
            }
            else if (_subscription == Subscription.Cold)
            {
                _subscription = _observable.Subscribe(box as IObserver<T> ?? Reference);
                throw new Exception("really cold");
            }
        }
        else
        {
            _continuation.Initialize(box);
            _subscription = _observable.Subscribe(box as IObserver<T> ?? Reference);
        }
    }

    public void OnCompleted() =>
        _continuation.OnCompleted();

    public void OnError(Exception error) =>
        _continuation.OnError(error);

    public void OnNext(T value) =>
        _continuation.OnNext(value);
}

internal interface ISubscribeStateMachine<T> : IStateMachine<T> { }