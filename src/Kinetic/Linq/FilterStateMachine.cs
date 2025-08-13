using System;
using Kinetic.Runtime;

namespace Kinetic.Linq;

internal struct FilterStateMachine<TContinuation, TFilter, TSource> : IStateMachine<TSource>
    where TContinuation : struct, IStateMachine<TSource>
    where TFilter : struct, ITransform<TSource, bool>
{
    private TContinuation _continuation;
    private TFilter _filter;

    public FilterStateMachine(TContinuation continuation, TFilter filter)
    {
        _continuation = continuation;
        _filter = filter;
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
        bool matches;
        try
        {
            matches = _filter.Transform(value);
        }
        catch (Exception error)
        {
            _continuation.OnError(error);
            return;
        }

        if (matches)
            _continuation.OnNext(value);
    }
}