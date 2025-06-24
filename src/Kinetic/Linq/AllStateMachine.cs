using System;
using Kinetic.Runtime;

namespace Kinetic.Linq;

internal struct AllStateMachine<TContinuation> : IStateMachine<bool>
    where TContinuation : struct, IStateMachine<bool>
{
    private TContinuation _continuation;

    public AllStateMachine(TContinuation continuation) =>
        _continuation = continuation;

    public StateMachineBox Box =>
        _continuation.Box;

    public StateMachineReference<bool> Reference =>
        StateMachineReference<bool>.Create(ref this);

    public StateMachineReference? Continuation =>
        _continuation.Reference;

    public void Initialize(StateMachineBox box) =>
        _continuation.Initialize(box);

    public void Dispose() =>
        _continuation.Dispose();

    public void OnNext(bool value)
    {
        if (!value)
        {
            _continuation.OnNext(false);
            _continuation.OnCompleted();
        }
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