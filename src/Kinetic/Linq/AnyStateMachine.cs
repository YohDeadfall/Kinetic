using System;
using Kinetic.Runtime;

namespace Kinetic.Linq;

internal struct AnyStateMachine<TContinuation, TSource> : IStateMachine<TSource>
    where TContinuation : struct, IStateMachine<bool>
{
    private TContinuation _continuation;

    public AnyStateMachine(TContinuation continuation) =>
        _continuation = continuation;

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

    public void OnNext(TSource value)
    {
        _continuation.OnNext(true);
        _continuation.OnCompleted();
    }

    public void OnError(Exception error)
    {
        _continuation.OnError(error);
    }

    public void OnCompleted()
    {
        _continuation.OnNext(true);
        _continuation.OnCompleted();
    }
}