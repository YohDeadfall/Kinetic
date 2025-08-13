using System;
using Kinetic.Runtime;

namespace Kinetic.Linq;

internal struct FilterAwaitStateMachine<TContinuation, TFilter, TAwaiter, TSource> : IStateMachine<TSource>
    where TContinuation : struct, IStateMachine<TSource>
    where TFilter : struct, ITransform<TSource, TAwaiter>
    where TAwaiter : struct, IAwaiter<bool>
{
    private TContinuation _continuation;
    private TFilter _filter;

    public FilterAwaitStateMachine(TContinuation continuation, TFilter filter)
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
            var awaiter = _filter.Transform(value);
            if (awaiter.IsCompleted)
            {
                matches = awaiter.GetResult();
            }
            else
            {
                awaiter.OnCompleted(CreateCompletion(awaiter, value));
                return;
            }
        }
        catch (Exception error)
        {
            _continuation.OnError(error);
            return;
        }

        if (matches)
            _continuation.OnNext(value);
    }

    private Action CreateCompletion(TAwaiter awaiter, TSource value)
    {
        var self = StateMachineValueReference<TSource>.Create(ref this);
        return () =>
        {
            bool matches;
            try
            {
                matches = awaiter.GetResult();
            }
            catch (Exception error)
            {
                self.Target.OnError(error);
                return;
            }

            if (matches)
                self.Target.OnNext(value);
        };
    }
}