using System;
using Kinetic.Runtime;

namespace Kinetic.Linq;

internal struct TransformStateMachine<TContinuation, TTransform, TSource, TResult> : IStateMachine<TSource>
    where TContinuation : struct, IStateMachine<TResult>
    where TTransform : struct, ITransform<TSource, TResult>
{
    private TContinuation _continuation;
    private TTransform _transfrom;

    public TransformStateMachine(TContinuation continuation, TTransform transform)
    {
        _continuation = continuation;
        _transfrom = transform;
    }

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
        TResult result;
        try
        {
            result = _transfrom.Transform(value);
        }
        catch (Exception error)
        {
            _continuation.OnError(error);
            return;
        }

        _continuation.OnNext(result);
    }
}