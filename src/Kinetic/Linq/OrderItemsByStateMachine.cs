using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Kinetic.Runtime;

namespace Kinetic.Linq;

internal struct OrderItemsByStateMachine<TContinuation, TState, TStateManager, TSource, TKey> :
    IStateMachine<ListChange<TSource>>,
    IOrderItemsByStateMachine<TState, TSource, TKey>
    where TContinuation : struct, IStateMachine<ListChange<TSource>>
    where TState : IOrderItemsByState<TKey>
    where TStateManager : IOrderItemsByStateManager<TState, TSource, TKey>
{
    private TContinuation _continuation;
    private TStateManager _itemManager;
    private TypedReference? _reference;

    private readonly IComparer<TState>? _itemComparer;
    private readonly List<TState> _items = new();
    private readonly List<int> _indexes = new();

    public OrderItemsByStateMachine(TContinuation continuation, TStateManager itemManager, IComparer<TKey>? comparer)
    {
        _continuation = continuation;
        _itemManager = itemManager;
        _itemComparer = comparer is null
            ? null
            : typeof(TKey).IsValueType
                ? comparer == Comparer<TKey>.Default ? null : new OrderItemsByStateComparer<TState, TKey>(comparer)
                : new OrderItemsByStateComparer<TState, TKey>(comparer ?? Comparer<TKey>.Default);
    }

    public StateMachineBox Box =>
        _continuation.Box;

    private TypedReference Reference =>
        _reference ??= Continuation is IReadOnlyList<TSource> items
            ? new TypedListReference(ref this, items)
            : new TypedReference(ref this);

    StateMachineReference<ListChange<TSource>> IStateMachine<ListChange<TSource>>.Reference =>
        Reference;

    IOrderItemsByStateMachine<TState, TSource, TKey> IOrderItemsByStateMachine<TState, TSource, TKey>.Reference =>
        Reference;

    public StateMachineReference? Continuation =>
        _continuation.Reference;

    public void Dispose()
    {
        _itemManager.DisposeItems(CollectionsMarshal.AsSpan(_items));
        _continuation.Dispose();
    }

    public void Initialize(StateMachineBox box) =>
        _continuation.Initialize(box);

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
                    _itemManager.DisposeItems(CollectionsMarshal.AsSpan(_items));
                    _items.Clear();
                    _indexes.Clear();

                    _continuation.OnNext(value);
                    break;
                }
            case ListChangeAction.Remove
            when value.OldIndex is var originalIndex:
                {
                    var index = _indexes[originalIndex];
                    var item = _items[index];

                    _itemManager.DisposeItem(item);
                    _items.RemoveAt(index);
                    _indexes.RemoveAt(originalIndex);

                    var indexes = CollectionsMarshal.AsSpan(_indexes);
                    foreach (ref var current in indexes)
                    {
                        if (current > index)
                            current -= 1;

                        var offset = Unsafe.ByteOffset(ref indexes[0], ref current).ToInt32() / Unsafe.SizeOf<int>();
                        if (offset > originalIndex)
                            _items[current].Index = offset;
                    }

                    _continuation.OnNext(
                        ListChange.Remove<TSource>(index));
                    break;
                }
            case ListChangeAction.Insert
            when value.NewIndex is var originalIndex:
                {
                    var item = _itemManager.CreateItem(originalIndex, value.NewItem, ref this);
                    var index = _items.BinarySearch(item, _itemComparer);

                    if (index < 0)
                        index = ~index;

                    var indexes = CollectionsMarshal.AsSpan(_indexes);
                    foreach (ref var current in indexes)
                    {
                        if (current >= index)
                            current += 1;

                        var offset = Unsafe.ByteOffset(ref indexes[0], ref current).ToInt32() / Unsafe.SizeOf<int>();
                        if (offset > originalIndex)
                            _items[current].Index = offset;
                    }

                    _indexes.Insert(originalIndex, index);
                    _items.Insert(index, item);

                    _continuation.OnNext(
                        ListChange.Insert(index, value.NewItem));
                    break;
                }
            case ListChangeAction.Replace
            when value.NewIndex is var originalIndex:
                {
                    var oldIndex = _indexes[originalIndex];
                    var oldItem = _items[oldIndex];

                    _itemManager.DisposeItem(oldItem);

                    var newItem = _itemManager.CreateItem(originalIndex, value.NewItem, ref this);
                    var newIndex = _items.BinarySearch(newItem, _itemComparer);

                    if (newIndex < 0)
                        newIndex = ~newIndex;

                    if (oldIndex == newIndex)
                    {
                        _items[oldIndex] = newItem;
                        _continuation.OnNext(ListChange.Replace(oldIndex, value.NewItem));
                    }
                    else
                    {
                        newIndex = UpdateIndexes(oldIndex, newIndex);

                        _indexes[value.OldIndex] = newIndex;

                        // TODO: Could be optimized using arrow manipulations
                        _items.RemoveAt(oldIndex);
                        _items.Insert(newIndex, newItem);
                        _continuation.OnNext(ListChange.Remove<TSource>(oldIndex));
                        _continuation.OnNext(ListChange.Insert(newIndex, value.NewItem));
                    }
                    break;
                }
            case ListChangeAction.Move:
                {
                    var index = _indexes[value.OldIndex];
                    // TODO: Could be optimized using arrow manipulations
                    _indexes.RemoveAt(value.OldIndex);
                    _indexes.Insert(value.NewIndex, index);
                    _items[index].Index = value.NewIndex;
                    break;
                }
        }
    }

    public void UpdateItem(int index, TState item)
    {
        // The index search uses two separate BinarySearch calls
        // to exclude the current item which already has a new key.
        var oldIndex = _items.IndexOf(item);
        var newIndex = oldIndex;

        if (oldIndex > 0)
        {
            newIndex = _items.BinarySearch(index: 0, count: oldIndex, item, _itemComparer);

            if (newIndex < 0)
                newIndex = ~newIndex;
        }

        if (newIndex == oldIndex &&
            newIndex < _items.Count - 1)
        {
            newIndex = _items.BinarySearch(newIndex + 1, _items.Count - newIndex - 1, item, _itemComparer);

            if (newIndex < 0)
                newIndex = ~newIndex;
        }

        if (oldIndex != newIndex)
        {
            newIndex = UpdateIndexes(oldIndex, newIndex);
            // TODO: Could be optimized using arrow manipulations
            _items.RemoveAt(oldIndex);
            _items.Insert(newIndex, item);
            _indexes[item.Index] = newIndex;
            _continuation.OnNext(ListChange.Move<TSource>(oldIndex, newIndex));
        }
    }

    private int UpdateIndexes(int oldIndex, int newIndex)
    {
        var indexes = CollectionsMarshal.AsSpan(_indexes);
        if (newIndex > oldIndex)
        {
            foreach (ref var current in indexes)
            {
                if (current > oldIndex && current < newIndex)
                    current -= 1;
            }

            return newIndex - 1;
        }
        else
        {
            foreach (ref var current in indexes)
            {
                if (current >= newIndex && current < oldIndex)
                    current += 1;
            }

            return newIndex;
        }
    }

    private class TypedReference :
        StateMachineReference<ListChange<TSource>, OrderItemsByStateMachine<TContinuation, TState, TStateManager, TSource, TKey>>,
        IOrderItemsByStateMachine<TState, TSource, TKey>
    {
        public TypedReference(ref OrderItemsByStateMachine<TContinuation, TState, TStateManager, TSource, TKey> stateMachine) :
            base(ref stateMachine)
        {
        }

        public IOrderItemsByStateMachine<TState, TSource, TKey> Reference =>
            this;

        public void UpdateItem(int index, TState item) =>
            Target.UpdateItem(index, item);
    }

    private class TypedListReference : TypedReference, IReadOnlyList<TSource>
    {
        private readonly IReadOnlyList<TSource> _items;

        public TypedListReference(ref OrderItemsByStateMachine<TContinuation, TState, TStateManager, TSource, TKey> stateMachine, IReadOnlyList<TSource> items) :
            base(ref stateMachine) =>
            _items = items;

        public TSource this[int index] =>
            _items[Target._items[index].Index];

        public int Count =>
            _items.Count;

        public IEnumerator<TSource> GetEnumerator() =>
            _items.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() =>
            _items.GetEnumerator();
    }
}