using System;
using System.Collections.Generic;
using Kinetic.Linq.StateMachines;

namespace Kinetic.Linq;

public static partial class ObservableView
{
    public static ObserverBuilder<ListChange<TSource>> OnItemAdded<TSource>(this ObserverBuilder<ListChange<TSource>> source, Action<TSource> action) =>
        source.ContinueWith<OnItemAddedStateMachineFactory<TSource>, ListChange<TSource>>(new() { Action = action });

    public static ObserverBuilder<ListChange<TSource>> OnItemAdded<TSource>(this ReadOnlyObservableList<TSource> source, Action<TSource> action) =>
        source.Changed.ToBuilder().OnItemAdded(action);

    private struct OnItemAddedStateMachineFactory<TSource> : IStateMachineFactory<ListChange<TSource>, ListChange<TSource>>
    {
        public required Action<TSource> Action { get; init; }

        public void Create<TContinuation>(in TContinuation continuation, ObserverStateMachine<ListChange<TSource>> source)
            where TContinuation : struct, IStateMachine<ListChange<TSource>>
        {
            source.ContinueWith<OnItemAddedStateMachine<TSource, TContinuation>>(new(continuation, Action));
        }
    }

    private struct OnItemAddedStateMachine<TSource, TContinuation> : IStateMachine<ListChange<TSource>>
        where TContinuation : struct, IStateMachine<ListChange<TSource>>
    {
        private TContinuation _continuation;
        private readonly Action<TSource> _action;

        public OnItemAddedStateMachine(in TContinuation continuation, Action<TSource> action)
        {
            _continuation = continuation;
            _action = action;
        }

        public StateMachineBox Box =>
            _continuation.Box;

        public StateMachine<ListChange<TSource>> Reference =>
            _continuation.Reference is IReadOnlyList<TSource> list
            ? new ListProxyStateMachine<TSource, OnItemAddedStateMachine<TSource, TContinuation>>(ref this, list)
            : StateMachine<ListChange<TSource>>.Create(ref this);

        public StateMachine? Continuation =>
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
                _action(value.NewItem);

            _continuation.OnNext(value);
        }
    }
}