using System;
using Kinetic.Runtime;

namespace Kinetic.Linq;

internal struct OfTypeStateMachine<TContinuation, TSource, TResult> : IStateMachine<TSource>
    where TContinuation : struct, IStateMachine<TResult>
{
    private TContinuation _continuation;

    public OfTypeStateMachine(TContinuation continuation) =>
        _continuation = continuation;

    public StateMachineBox Box =>
        _continuation.Box;

    public StateMachineReference<TSource> Reference =>
        StateMachineReference<TSource>.Create(ref this);

    public StateMachineReference? Continuation =>
        _continuation.Reference;

    public void Dispose() =>
        _continuation.Dispose();

    public void Initialize(StateMachineBox box) =>
        _continuation.Initialize(box);

    public void OnCompleted() =>
        _continuation.OnCompleted();

    public void OnError(Exception error) =>
        _continuation.OnError(error);

    public void OnNext(TSource value)
    {
        if (value is TResult result)
            _continuation.OnNext(result);
    }
}