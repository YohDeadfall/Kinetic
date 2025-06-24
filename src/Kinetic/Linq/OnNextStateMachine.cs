using System;
using Kinetic.Runtime;

namespace Kinetic.Linq;

internal struct OnNextStateMachine<TContinuation, TSource> : IStateMachine<TSource>
    where TContinuation : struct, IStateMachine<TSource>
{
    private TContinuation _continuation;
    private readonly Action<TSource> _onNext;

    public OnNextStateMachine(TContinuation continuation, Action<TSource> onNext)
    {
        _continuation = continuation;
        _onNext = onNext;
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

    public void OnError(Exception error) =>
        _continuation.OnError(error);

    public void OnNext(TSource value)
    {
        try
        {
            _onNext(value);
        }
        catch (Exception error)
        {
            _continuation.OnError(error);
            return;
        }

        _continuation.OnNext(value);
    }
}