using System;
using Kinetic.Runtime;

namespace Kinetic.Linq;

internal struct ThrottleStateMachine<TContinuation, TSource> : IStateMachine<TSource>
    where TContinuation : struct, IStateMachine<TSource>
{
    private TContinuation _continuation;
    private ThrottleStateMachinePublisher<TSource> _publisher;

    public ThrottleStateMachine(TContinuation continuation, TimeSpan delay, bool continueOnCapturedContext)
    {
        _continuation = continuation;
        _publisher = new ThrottleStateMachinePublisher<TSource>(delay, continueOnCapturedContext);
    }

    public StateMachineBox Box =>
        _continuation.Box;

    public StateMachineReference<TSource> Reference =>
        StateMachineReference<TSource>.Create(ref this);

    public StateMachineReference? Continuation =>
        _continuation.Reference;

    public void Initialize(StateMachineBox box) =>
        _continuation.Initialize(box);

    public void Dispose()
    {
        _publisher?.Dispose();
        _continuation.Dispose();
    }

    public void OnCompleted() =>
        _continuation.OnCompleted();

    public void OnError(Exception error) =>
        _continuation.OnError(error);

    public void OnNext(TSource value) =>
        _publisher.OnNext(value);
}