using System;
using Kinetic.Runtime;

namespace Kinetic.Linq;

internal struct OnCompletedStateMachine<TContinuation, TSource> : IStateMachine<TSource>
    where TContinuation : struct, IStateMachine<TSource>
{
    private TContinuation _continuation;
    private readonly Action _onCompleted;

    public OnCompletedStateMachine(TContinuation continuation, Action onCompleted)
    {
        _continuation = continuation;
        _onCompleted = onCompleted;
    }

    public StateMachineBox Box =>
        _continuation.Box;

    public StateMachineReference<TSource> Reference =>
        StateMachineReference<TSource>.Create(ref this);

    public StateMachineReference? Continuation =>
        _continuation.Reference;

    public void Initialize(StateMachineBox box) =>
        _continuation.Initialize(box);

    public void Dispose() =>
        _continuation.Dispose();

    public void OnCompleted()
    {
        try
        {
            _onCompleted();
        }
        catch (Exception error)
        {
            _continuation.OnError(error);
            return;
        }

        _continuation.OnCompleted();
    }

    public void OnError(Exception error) =>
        _continuation.OnError(error);

    public void OnNext(TSource value) =>
        _continuation.OnNext(value);
}