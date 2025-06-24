using System;
using Kinetic.Runtime;

namespace Kinetic.Linq;

internal struct OnErrorStateMachine<TContinuation, TSource> : IStateMachine<TSource>
    where TContinuation : struct, IStateMachine<TSource>
{
    private TContinuation _continuation;
    private readonly Action<Exception> _onError;

    public OnErrorStateMachine(TContinuation continuation, Action<Exception> onError)
    {
        _continuation = continuation;
        _onError = onError;
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

    public void OnCompleted() =>
        _continuation.OnCompleted();

    public void OnError(Exception error)
    {
        try
        {
            _onError(error);
        }
        catch (Exception errorInner)
        {
            _continuation.OnError(errorInner);
            return;
        }

        _continuation.OnError(error);
    }

    public void OnNext(TSource value) =>
        _continuation.OnNext(value);
}