using System;
using System.Collections.Generic;
using Kinetic.Linq.StateMachines;

namespace Kinetic.Linq;

public static partial class ObservableView
{
    public static ObserverBuilder<ListChange<TSource>> OnItemRemoved<TSource>(this ObserverBuilder<ListChange<TSource>> source, Action<TSource> action) =>
        source.ContinueWith<OnItemRemovedStateMachineFactory<TSource>, ListChange<TSource>>(new() { Action = action });

    public static ObserverBuilder<ListChange<TSource>> OnItemRemoved<TSource>(this ReadOnlyObservableList<TSource> source, Action<TSource> action) =>
        source.Changed.ToBuilder().OnItemRemoved(action);

    private struct OnItemRemovedStateMachineFactory<TSource> : IStateMachineFactory<ListChange<TSource>, ListChange<TSource>>
    {
        public required Action<TSource> Action { get; init; }

        public void Create<TContinuation>(in TContinuation continuation, ObserverStateMachine<ListChange<TSource>> source)
            where TContinuation : struct, IStateMachine<ListChange<TSource>>
        {
            source.ContinueWith<OnItemRemovedStateMachine<TSource, TContinuation>>(new(continuation, Action));
        }
    }

    private struct OnItemRemovedStateMachine<TSource, TContinuation> : IStateMachine<ListChange<TSource>>
        where TContinuation : struct, IStateMachine<ListChange<TSource>>
    {
        private TContinuation _continuation;
        private readonly Action<TSource> _action;
        private readonly List<TSource> _items = new();

        public OnItemRemovedStateMachine(in TContinuation continuation, Action<TSource> action)
        {
            _continuation = continuation;
            _action = action;
        }

        public StateMachineBox Box =>
            _continuation.Box;

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
            _continuation.OnNext(value);

            switch (value.Action)
            {
                case ListChangeAction.RemoveAll:
                    {
                        foreach (var item in _items)
                            _action(item);

                        _items.Clear();

                        break;
                    }
                case ListChangeAction.Remove:
                    {
                        var index = value.OldIndex;
                        var item = _items[value.OldIndex];

                        _action(item);
                        _items.RemoveAt(index);

                        break;
                    }
                case ListChangeAction.Insert:
                    {
                        _items.Insert(
                            value.NewIndex,
                            value.NewItem);

                        break;
                    }
                case ListChangeAction.Replace:
                    {
                        var index = value.OldIndex;
                        var item = _items[index];

                        _action(item);
                        _items[index] = value.NewItem;

                        break;
                    }
                case ListChangeAction.Move:
                    {
                        var index = value.OldIndex;
                        var item = _items[index];

                        _items.RemoveAt(index);
                        _items.Insert(index, item);

                        break;
                    }
            }
        }
    }
}