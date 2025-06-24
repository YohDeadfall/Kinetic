using System;
using Kinetic.Runtime;

namespace Kinetic.Linq;

internal struct AggregateStateMachine<TContinuation, TAccumulator, TSource, TResult> : IStateMachine<TSource>
    where TContinuation : struct, IStateMachine<TResult>
    where TAccumulator : struct, IAccumulator<TSource, TResult>
{
    private TContinuation _continuation;
    private TAccumulator _accumulator;

    public AggregateStateMachine(TContinuation continuation, TAccumulator accumulator)
    {
        _continuation = continuation;
        _accumulator = accumulator;
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

    public void OnCompleted()
    {
        try
        {
            _accumulator.Publish(ref _continuation);
        }
        catch (Exception error)
        {
            _continuation.OnError(error);
            return;
        }

        _continuation.OnCompleted();
    }

    public void OnError(Exception error)
    {
        _continuation.OnError(error);
    }

    public void OnNext(TSource value)
    {
        try
        {
            if (_accumulator.Accumulate(value))
                return;
        }
        catch (Exception error)
        {
            _continuation.OnError(error);
        }

        OnCompleted();
    }
}