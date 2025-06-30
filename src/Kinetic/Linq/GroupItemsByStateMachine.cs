using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Kinetic.Runtime;

namespace Kinetic.Linq;

internal struct GroupItemsByStateMachine<TContinuation, TState, TStateManager, TSource, TKey> :
    IGroupItemsByStateMachine<TState, TSource, TKey>,
    IStateMachine<ListChange<TSource>>
    where TContinuation : IStateMachine<ListChange<IGrouping<TKey, ListChange<TSource>>>>
    where TState : IGroupItemsByState
    where TStateManager : IGroupItemsByStateManager<TState, TSource, TKey>
{
    private TContinuation _continuation;
    private TStateManager _itemManager;
    private TypedReference? _reference;

    private readonly List<TState> _items = new();
    private readonly List<ListGrouping<TKey, TSource>?> _groups = new();
    private readonly IEqualityComparer<TKey>? _comparer;

    public GroupItemsByStateMachine(TContinuation continuation, TStateManager itemManager, IEqualityComparer<TKey>? comparer)
    {
        _continuation = continuation;
        _itemManager = itemManager;
        _comparer = typeof(TKey).IsValueType
            ? comparer == EqualityComparer<TKey>.Default ? null : comparer
            : comparer ?? EqualityComparer<TKey>.Default;
    }

    public StateMachineBox Box =>
        _continuation.Box;

    private TypedReference Reference =>
        _reference ??= new TypedReference(ref this);

    StateMachineReference<ListChange<TSource>> IStateMachine<ListChange<TSource>>.Reference =>
        Reference;

    IGroupItemsByStateMachine<TState, TSource, TKey> IGroupItemsByStateMachine<TState, TSource, TKey>.Reference =>
        Reference;

    public StateMachineReference? Continuation =>
        _continuation.Reference;

    public void Dispose()
    {
        _itemManager.DisposeAll(CollectionsMarshal.AsSpan(_items));
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
                    _itemManager.DisposeAll(CollectionsMarshal.AsSpan(_items));
                    _items.Clear();
                    _groups.Clear();
                    _continuation.OnNext(ListChange.RemoveAll<IGrouping<TKey, ListChange<TSource>>>());
                    break;
                }
            case ListChangeAction.Remove:
                {
                    var index = value.OldIndex;
                    var item = _items[index];

                    RemoveItemFromGroup(index, item);

                    _items.RemoveAt(index);
                    _itemManager.Dispose(item);
                    _itemManager.SetOriginalIndexes(
                        items: CollectionsMarshal.AsSpan(_items).Slice(index),
                        indexChange: -1);
                    break;
                }
            case ListChangeAction.Insert:
            case ListChangeAction.Replace:
                {
                    try
                    {
                        _itemManager.Create(
                            value.NewIndex,
                            value.NewItem,
                            ref this,
                            value.Action == ListChangeAction.Replace);
                    }
                    catch (Exception error)
                    {
                        _continuation.OnError(error);
                    }
                    break;
                }
            case ListChangeAction.Move:
                {
                    var newIndex = value.NewIndex;
                    var oldIndex = value.OldIndex;
                    var item = _items[oldIndex];

                    _items.RemoveAt(oldIndex);
                    _items.Insert(newIndex, item);

                    var (start, length, indexChange) = newIndex > oldIndex
                        ? (oldIndex, newIndex - oldIndex, -1)
                        : (newIndex, oldIndex - newIndex, 1);

                    var items = CollectionsMarshal.AsSpan(_items).Slice(start, length);

                    _itemManager.SetOriginalIndexes(items, indexChange);
                    _itemManager.SetOriginalIndex(item, newIndex);
                    break;
                }
        }
    }

    public void AddItemDeferred(int index, TState item) =>
        _items.Insert(index, item);

    public void AddItem(int index, TState item, TSource source, TKey key)
    {
        var (grouping, groupingIndex) = GetGrouping(key);

        item.Group = groupingIndex;
        item.Index = grouping.Add(source);

        _items.Insert(index, item);
        _itemManager.SetOriginalIndexes(
            items: CollectionsMarshal.AsSpan(_items).Slice(index),
            indexChange: 1);
    }

    public void UpdateItem(int index, TState item, TSource source, TKey key)
    {
        var (grouping, groupingIndex) = GetGrouping(key);

        if (groupingIndex != item.Group)
        {
            RemoveItemFromGroup(index, item);

            item.Group = groupingIndex;
            item.Index = grouping.Add(source);

            if (typeof(TState).IsValueType)
                _items[index] = item;
        }
    }

    public void ReplaceItem(int index, TState item, TSource source, TKey key)
    {
        var oldItem = _items[index];
        var (grouping, groupingIndex) = GetGrouping(key);

        if (groupingIndex == oldItem.Group)
        {
            item.Group = oldItem.Group;
            item.Index = oldItem.Index;

            _itemManager.Dispose(oldItem);
            _items[index] = item;

            grouping.Replace(item.Group, source);
        }
        else
        {
            RemoveItemFromGroup(index, oldItem);

            item.Group = groupingIndex;
            item.Index = grouping.Add(source);

            _items[index] = item;
        }
    }

    private void RemoveItemFromGroup(int index, TState item)
    {
        var grouping = _groups[item.Group];
        var groupingIndex = item.Group;

        Debug.Assert(grouping is { });
        grouping.Remove(item.Index);
        item.Group = -1;
        item.Index = -1;

        if (typeof(TState).IsValueType)
            _items[index] = item;

        if (grouping.IsEmpty)
        {
            _groups[groupingIndex] = null;
            _continuation.OnNext(
                ListChange.Remove<IGrouping<TKey, ListChange<TSource>>>(groupingIndex));
        }
    }

    private (ListGrouping<TKey, TSource>, int) GetGrouping(TKey key)
    {
        var freeIndex = -1;
        var currentIndex = 0;

        var comparer = _comparer;
        if (comparer is null && typeof(TKey).IsValueType)
        {
            foreach (var grouping in _groups)
            {
                if (grouping is null)
                    freeIndex = currentIndex;
                else
                if (EqualityComparer<TKey>.Default.Equals(grouping.Key, key))
                    return (grouping, currentIndex);

                currentIndex += 1;
            }
        }
        else
        {
            Debug.Assert(comparer is { });

            foreach (var grouping in _groups)
            {
                if (grouping is null)
                    freeIndex = currentIndex;
                else
                if (comparer.Equals(grouping.Key, key))
                    return (grouping, currentIndex);

                currentIndex += 1;
            }
        }

        {
            var grouping = new ListGrouping<TKey, TSource>
            {
                Key = key,
            };

            if (freeIndex == -1)
            {
                freeIndex = _groups.Count;
                _groups.Add(grouping);
            }
            else
            {
                _groups[freeIndex] = grouping;
            }

            _continuation.OnNext(ListChange.Insert<IGrouping<TKey, ListChange<TSource>>>(freeIndex, grouping));

            return (grouping, freeIndex);
        }
    }

    private sealed class TypedReference :
        StateMachineReference<ListChange<TSource>, GroupItemsByStateMachine<TContinuation, TState, TStateManager, TSource, TKey>>,
        IGroupItemsByStateMachine<TState, TSource, TKey>
    {
        public TypedReference(ref GroupItemsByStateMachine<TContinuation, TState, TStateManager, TSource, TKey> stateMachine) :
            base(ref stateMachine)
        {
        }

        public IGroupItemsByStateMachine<TState, TSource, TKey> Reference =>
            this;

        public void AddItemDeferred(int index, TState item) =>
            Target.AddItemDeferred(index, item);

        public void AddItem(int index, TState item, TSource source, TKey key) =>
            Target.AddItem(index, item, source, key);

        public void ReplaceItem(int index, TState item, TSource source, TKey key) =>
            Target.ReplaceItem(index, item, source, key);

        public void UpdateItem(int index, TState item, TSource source, TKey key) =>
            Target.UpdateItem(index, item, source, key);
    }
}