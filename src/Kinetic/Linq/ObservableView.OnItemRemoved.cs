using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

    private struct OnItemRemovedStateMachine<TSource, TContinuation> : IStateMachine<ListChange<TSource>>, IReadOnlyList<TSource>
        where TContinuation : struct, IStateMachine<ListChange<TSource>>
    {
        private TContinuation _continuation;
        private IReadOnlyList<TSource>? _items;
        private readonly Action<TSource> _action;

        public OnItemRemovedStateMachine(in TContinuation continuation, Action<TSource> action)
        {
            _continuation = continuation;
            _action = action;
        }

        public StateMachineBox Box =>
            _continuation.Box;

        public int Count =>
            _items!.Count;

        public StateMachine<ListChange<TSource>> Reference =>
            new ListStateMachine<TSource, OnItemRemovedStateMachine<TSource, TContinuation>>(ref this);

        public StateMachine? Continuation =>
            _continuation.Reference;

        public TSource this[int index] =>
            _items![index];

        public StateMachine<ListChange<TSource>> GetReference() =>
            new ListStateMachine<TSource, OnItemRemovedStateMachine<TSource, TContinuation>>(ref this);

        public void Initialize(StateMachineBox box)
        {
            _continuation.Initialize(box);
            _items = _continuation.Reference as IReadOnlyList<TSource> ?? new List<TSource>();
        }

        public void Dispose() =>
            _continuation.Dispose();

        public void OnCompleted() =>
            _continuation.OnCompleted();

        public void OnError(Exception error) =>
            _continuation.OnError(error);

        public void OnNext(ListChange<TSource> value)
        {
            switch (value.Action)
            {
                case ListChangeAction.RemoveAll:
                    {
                        var buffer = _items!.ToArray();

                        _continuation.OnNext(value);

                        foreach (var item in buffer)
                            _action(item);

                        if (_items is List<TSource> items)
                            items.Clear();

                        break;
                    }
                case ListChangeAction.Remove:
                    {
                        var index = value.OldIndex;
                        var item = _items![value.OldIndex];

                        _continuation.OnNext(value);
                        _action(item);

                        if (_items is List<TSource> items)
                            items.RemoveAt(index);

                        break;
                    }
                case ListChangeAction.Insert:
                    {
                        _continuation.OnNext(value);

                        if (_items is List<TSource> items)
                            items.Insert(value.NewIndex, value.NewItem);

                        break;
                    }
                case ListChangeAction.Replace:
                    {
                        var index = value.OldIndex;
                        var item = _items![index];

                        _continuation.OnNext(value);
                        _action(item);

                        if (_items is List<TSource> items)
                            items[index] = value.NewItem;

                        break;
                    }
                case ListChangeAction.Move:
                    {
                        _continuation.OnNext(value);

                        if (_items is List<TSource> items)
                        {
                            var index = value.OldIndex;
                            var item = items[index];

                            items.RemoveAt(index);
                            items.Insert(index, item);
                        }

                        break;
                    }
            }
        }

        public IEnumerator<TSource> GetEnumerator() =>
            _items!.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() =>
            _items!.GetEnumerator();
    }
}