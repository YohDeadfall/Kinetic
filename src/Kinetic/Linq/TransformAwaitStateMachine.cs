using System;
using Kinetic.Runtime;

namespace Kinetic.Linq;

internal struct TransformAwaitStateMachine<TContinuation, TTransform, TAwaiter, TSource, TResult> : IStateMachine<TSource>
    where TContinuation : struct, IStateMachine<TResult>
    where TTransform : struct, ITransform<TSource, TAwaiter>
    where TAwaiter : struct, IAwaiter<TResult>
{
    private TContinuation _continuation;
    private TTransform _transform;

    public TransformAwaitStateMachine(TContinuation continuation, TTransform transform)
    {
        _continuation = continuation;
        _transform = transform;
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
            var awaiter = _transform.Transform(value);
            if (awaiter.IsCompleted)
            {
                result = awaiter.GetResult();
            }
            else
            {
                awaiter.OnCompleted(CreateCompletion(awaiter));
                return;
            }
        }
        catch (Exception error)
        {
            _continuation.OnError(error);
            return;
        }

        _continuation.OnNext(result);
    }

    private Action CreateCompletion(TAwaiter awaiter)
    {
        var self = StateMachineValueReference<TSource>.Create(ref this);
        return () =>
        {
            TResult result;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception error)
            {
                self.Target._continuation.OnError(error);
                return;
            }

            self.Target._continuation.OnNext(result);
        };
    }
}