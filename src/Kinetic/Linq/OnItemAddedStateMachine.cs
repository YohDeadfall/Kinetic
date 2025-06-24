using System;
using System.Collections.Generic;
using Kinetic.Runtime;

namespace Kinetic.Linq;

internal struct OnItemAddedStateMachine<TContinuation, TSource> : IStateMachine<ListChange<TSource>>
    where TContinuation : struct, IStateMachine<ListChange<TSource>>
{
    private TContinuation _continuation;
    private readonly Action<TSource> _onAdded;

    public OnItemAddedStateMachine(in TContinuation continuation, Action<TSource> onAdded)
    {
        _continuation = continuation;
        _onAdded = onAdded;
    }

    public StateMachineBox Box =>
        _continuation.Box;

    public StateMachineReference<ListChange<TSource>> Reference =>
        _continuation.Reference is IReadOnlyList<TSource> list
        ? new ListProxyStateMachineReference<TSource, OnItemAddedStateMachine<TContinuation, TSource>>(ref this, list)
        : StateMachineReference<ListChange<TSource>>.Create(ref this);

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

    public void OnNext(ListChange<TSource> value)
    {
        if (value.Action is ListChangeAction.Insert or ListChangeAction.Replace)
            try
            {
                _onAdded(value.NewItem);
            }
            catch (Exception error)
            {
                _continuation.OnError(error);
                return;
            }

        _continuation.OnNext(value);
    }
}