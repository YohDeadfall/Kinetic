using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Kinetic.Linq.StateMachines;

namespace Kinetic.Linq;

public static partial class ObservableView
{
    public static ObserverBuilder<ListChange<ObservableGrouping<TKey, TSource>>> GroupBy<TSource, TKey>(
        this ObserverBuilder<ListChange<TSource>> source, Func<TSource, TKey> keySelector)
    {
        return source.GroupBy(keySelector, resultSelector: ObservableGrouping.Create);
    }

    public static ObserverBuilder<ListChange<ObservableGrouping<TKey, TSource>>> GroupBy<TSource, TKey>(
        this ObserverBuilder<ListChange<TSource>> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey> keyComparer)
    {
        return source.GroupBy(keySelector, resultSelector: ObservableGrouping.Create, keyComparer);
    }

    public static ObserverBuilder<ListChange<ObservableGrouping<TKey, TResult>>> GroupBy<TSource, TKey, TResult>(
        this ObserverBuilder<ListChange<TSource>> source,
        Func<TSource, TKey> keySelector,
        Func<ObserverBuilder<ListChange<TSource>>, ObserverBuilder<ListChange<TResult>>> resultSelector)
    {
        return source.GroupBy(keySelector, (k, b) => ObservableGrouping.Create(k, resultSelector(b)));
    }

    public static ObserverBuilder<ListChange<ObservableGrouping<TKey, TResult>>> GroupBy<TSource, TKey, TResult>(
        this ObserverBuilder<ListChange<TSource>> source,
        Func<TSource, TKey> keySelector,
        Func<ObserverBuilder<ListChange<TSource>>, ObserverBuilder<ListChange<TResult>>> resultSelector,
        IEqualityComparer<TKey> keyComparer)
    {
        return source.GroupBy(keySelector, (k, b) => ObservableGrouping.Create(k, resultSelector(b)), keyComparer);
    }

    public static ObserverBuilder<ListChange<TResult>> GroupBy<TSource, TKey, TResult>(
        this ObserverBuilder<ListChange<TSource>> source,
        Func<TSource, TKey> keySelector,
        Func<TKey, ObserverBuilder<ListChange<TSource>>, TResult> resultSelector)
    {
        return source.ContinueWith<GroupByStateMachineFactory<TKey, TSource, TResult>, ListChange<TResult>>(
            new(keyComparer: null, keySelector, resultSelector));
    }

    public static ObserverBuilder<ListChange<TResult>> GroupBy<TSource, TKey, TResult>(
        this ObserverBuilder<ListChange<TSource>> source,
        Func<TSource, TKey> keySelector,
        Func<TKey, ObserverBuilder<ListChange<TSource>>, TResult> resultSelector,
        IEqualityComparer<TKey> keyComparer)
    {
        return source.ContinueWith<GroupByStateMachineFactory<TKey, TSource, TResult>, ListChange<TResult>>(
            new(keyComparer, keySelector, resultSelector));
    }

    public static ObserverBuilder<ListChange<ObservableGrouping<TKey, TSource>>> GroupBy<TSource, TKey>(
        this ReadOnlyObservableList<TSource> source, Func<TSource, TKey> keySelector)
    {
        return source.Changed.ToBuilder().GroupBy(keySelector, resultSelector: ObservableGrouping.Create);
    }

    public static ObserverBuilder<ListChange<ObservableGrouping<TKey, TSource>>> GroupBy<TSource, TKey>(
        this ReadOnlyObservableList<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey> keyComparer)
    {
        return source.Changed.ToBuilder().GroupBy(keySelector, resultSelector: ObservableGrouping.Create, keyComparer);
    }

    public static ObserverBuilder<ListChange<ObservableGrouping<TKey, TResult>>> GroupBy<TSource, TKey, TResult>(
        this ReadOnlyObservableList<TSource> source,
        Func<TSource, TKey> keySelector,
        Func<ObserverBuilder<ListChange<TSource>>, ObserverBuilder<ListChange<TResult>>> resultSelector)
    {
        return source.Changed.ToBuilder().GroupBy(keySelector, (k, b) => ObservableGrouping.Create(k, resultSelector(b)));
    }

    public static ObserverBuilder<ListChange<ObservableGrouping<TKey, TResult>>> GroupBy<TSource, TKey, TResult>(
        this ReadOnlyObservableList<TSource> source,
        Func<TSource, TKey> keySelector,
        Func<ObserverBuilder<ListChange<TSource>>, ObserverBuilder<ListChange<TResult>>> resultSelector,
        IEqualityComparer<TKey> keyComparer)
    {
        return source.Changed.ToBuilder().GroupBy(keySelector, (k, b) => ObservableGrouping.Create(k, resultSelector(b)), keyComparer);
    }

    public static ObserverBuilder<ListChange<TResult>> GroupBy<TSource, TKey, TResult>(
        this ReadOnlyObservableList<TSource> source,
        Func<TSource, TKey> keySelector,
        Func<TKey, ObserverBuilder<ListChange<TSource>>, TResult> resultSelector)
    {
        return source.Changed.ToBuilder().GroupBy(keySelector, resultSelector);
    }

    public static ObserverBuilder<ListChange<TResult>> GroupBy<TSource, TKey, TResult>(
        this ReadOnlyObservableList<TSource> source,
        Func<TSource, TKey> keySelector,
        Func<TKey, ObserverBuilder<ListChange<TSource>>, TResult> resultSelector,
        IEqualityComparer<TKey> keyComparer)
    {
        return source.Changed.ToBuilder().GroupBy(keySelector, resultSelector, keyComparer);
    }

    public static ObserverBuilder<ListChange<ObservableGrouping<TKey, TSource>>> GroupBy<TSource, TKey>(
        this ObserverBuilder<ListChange<TSource>> source, Func<TSource, ObserverBuilder<TKey>> keySelector)
    {
        return source.GroupBy(keySelector, ObservableGrouping.Create<TKey, TSource>);
    }

    public static ObserverBuilder<ListChange<ObservableGrouping<TKey, TSource>>> GroupBy<TSource, TKey>(
        this ObserverBuilder<ListChange<TSource>> source, Func<TSource, ObserverBuilder<TKey>> keySelector, IEqualityComparer<TKey> keyComparer)
    {
        return source.GroupBy(keySelector, resultSelector: ObservableGrouping.Create, keyComparer);
    }

    public static ObserverBuilder<ListChange<ObservableGrouping<TKey, TResult>>> GroupBy<TSource, TKey, TResult>(
        this ObserverBuilder<ListChange<TSource>> source,
        Func<TSource, ObserverBuilder<TKey>> keySelector,
        Func<ObserverBuilder<ListChange<TSource>>, ObserverBuilder<ListChange<TResult>>> resultSelector)
    {
        return source.GroupBy(keySelector, (k, b) => ObservableGrouping.Create<TKey, TResult>(k, resultSelector(b)));
    }

    public static ObserverBuilder<ListChange<ObservableGrouping<TKey, TResult>>> GroupBy<TSource, TKey, TResult>(
        this ObserverBuilder<ListChange<TSource>> source,
        Func<TSource, ObserverBuilder<TKey>> keySelector,
        Func<ObserverBuilder<ListChange<TSource>>, ObserverBuilder<ListChange<TResult>>> resultSelector,
        IEqualityComparer<TKey> keyComparer)
    {
        return source.GroupBy(keySelector, (k, b) => ObservableGrouping.Create(k, resultSelector(b)), keyComparer);
    }

    public static ObserverBuilder<ListChange<TResult>> GroupBy<TSource, TKey, TResult>(
        this ObserverBuilder<ListChange<TSource>> source,
        Func<TSource, ObserverBuilder<TKey>> keySelector,
        Func<TKey, ObserverBuilder<ListChange<TSource>>, TResult> resultSelector)
    {
        return source.ContinueWith<GroupByStateMachineFactory<TKey, TSource, TResult>, ListChange<TResult>>(
            new(keyComparer: null, keySelector, resultSelector));
    }

    public static ObserverBuilder<ListChange<TResult>> GroupBy<TSource, TKey, TResult>(
        this ObserverBuilder<ListChange<TSource>> source,
        Func<TSource, ObserverBuilder<TKey>> keySelector,
        Func<TKey, ObserverBuilder<ListChange<TSource>>, TResult> resultSelector,
        IEqualityComparer<TKey> keyComparer)
    {
        return source.ContinueWith<GroupByStateMachineFactory<TKey, TSource, TResult>, ListChange<TResult>>(
            new(keyComparer, keySelector, resultSelector));
    }

    public static ObserverBuilder<ListChange<ObservableGrouping<TKey, TSource>>> GroupBy<TSource, TKey>(
        this ReadOnlyObservableList<TSource> source, Func<TSource, Property<TKey>> keySelector)
    {
        return source.Changed.ToBuilder().GroupBy(s => keySelector(s).Changed.ToBuilder(), resultSelector: ObservableGrouping.Create<TKey, TSource>);
    }

    public static ObserverBuilder<ListChange<ObservableGrouping<TKey, TSource>>> GroupBy<TSource, TKey>(
        this ReadOnlyObservableList<TSource> source, Func<TSource, Property<TKey>> keySelector, IEqualityComparer<TKey> keyComparer)
    {
        return source.Changed.ToBuilder().GroupBy(s => keySelector(s).Changed.ToBuilder(), resultSelector: ObservableGrouping.Create, keyComparer);
    }

    public static ObserverBuilder<ListChange<ObservableGrouping<TKey, TResult>>> GroupBy<TSource, TKey, TResult>(
        this ReadOnlyObservableList<TSource> source,
        Func<TSource, Property<TKey>> keySelector,
        Func<ObserverBuilder<ListChange<TSource>>, ObserverBuilder<ListChange<TResult>>> resultSelector)
    {
        return source.Changed.ToBuilder().GroupBy(s => keySelector(s).Changed.ToBuilder(), (k, b) => ObservableGrouping.Create<TKey, TResult>(k, resultSelector(b)));
    }

    public static ObserverBuilder<ListChange<ObservableGrouping<TKey, TResult>>> GroupBy<TSource, TKey, TResult>(
        this ReadOnlyObservableList<TSource> source,
        Func<TSource, Property<TKey>> keySelector,
        Func<ObserverBuilder<ListChange<TSource>>, ObserverBuilder<ListChange<TResult>>> resultSelector,
        IEqualityComparer<TKey> keyComparer)
    {
        return source.Changed.ToBuilder().GroupBy(s => keySelector(s).Changed.ToBuilder(), (k, b) => ObservableGrouping.Create(k, resultSelector(b)), keyComparer);
    }

    public static ObserverBuilder<ListChange<TResult>> GroupBy<TSource, TKey, TResult>(
        this ReadOnlyObservableList<TSource> source,
        Func<TSource, Property<TKey>> keySelector,
        Func<TKey, ObserverBuilder<ListChange<TSource>>, TResult> resultSelector)
    {
        return source.Changed.ToBuilder().GroupBy(s => keySelector(s).Changed.ToBuilder(), resultSelector);
    }

    public static ObserverBuilder<ListChange<TResult>> GroupBy<TSource, TKey, TResult>(
        this ReadOnlyObservableList<TSource> source,
        Func<TSource, Property<TKey>> keySelector,
        Func<TKey, ObserverBuilder<ListChange<TSource>>, TResult> resultSelector,
        IEqualityComparer<TKey> keyComparer)
    {
        return source.Changed.ToBuilder().GroupBy(s => keySelector(s).Changed.ToBuilder(), resultSelector, keyComparer);
    }

    private readonly struct GroupByStateMachineFactory<TKey, TSource, TResult> : IStateMachineFactory<ListChange<TSource>, ListChange<TResult>>
    {
        private readonly IEqualityComparer<TKey>? _keyComparer;
        private readonly Delegate _keySelector;
        private readonly Func<TKey, ObserverBuilder<ListChange<TSource>>, TResult> _resultSelector;

        public GroupByStateMachineFactory(
            IEqualityComparer<TKey>? keyComparer,
            Func<TSource, TKey> keySelector,
            Func<TKey, ObserverBuilder<ListChange<TSource>>, TResult> resultSelector)
        {
            _keyComparer = keyComparer;
            _keySelector = keySelector;
            _resultSelector = resultSelector;
        }

        public GroupByStateMachineFactory(
            IEqualityComparer<TKey>? keyComparer,
            Func<TSource, ObserverBuilder<TKey>> keySelector,
            Func<TKey, ObserverBuilder<ListChange<TSource>>, TResult> resultSelector)
        {
            _keyComparer = keyComparer;
            _keySelector = keySelector;
            _resultSelector = resultSelector;
        }

        public void Create<TContinuation>(in TContinuation continuation, ObserverStateMachine<ListChange<TSource>> source)
            where TContinuation : struct, IStateMachine<ListChange<TResult>>
        {
            var keyComparer = typeof(TKey).IsValueType
                ? _keyComparer is { } && _keyComparer == EqualityComparer<TKey>.Default ? null : _keyComparer
                : _keyComparer ?? EqualityComparer<TKey>.Default;

            if (_keySelector is Func<TSource, TKey> staticKeySelector)
            {
                source.ContinueWith<GroupByStateMachine<TKey, TSource, TResult, GroupingStaticItem, GroupingStaticItem.Factory<TKey, TSource>, TContinuation>>(
                    new(continuation, new() { KeySelector = staticKeySelector }, keyComparer, _resultSelector));
            }
            else
            if (_keySelector is Func<TSource, ObserverBuilder<TKey>> dynamicKeySelector)
            {
                source.ContinueWith<GroupByStateMachine<TKey, TSource, TResult, GroupingDynamicItem<TSource>, GroupingDynamicItem<TSource>.Factory<TKey>, TContinuation>>(
                    new(continuation, new() { KeySelector = dynamicKeySelector }, keyComparer, _resultSelector));
            }
            else
            {
                throw new NotSupportedException();
            }
        }
    }

    private struct GroupByStateMachine<TKey, TSource, TResult, TItem, TItemFactory, TContinuation> : IGroupByStateMachine<TKey, TSource, TItem>
        where TItem : IGroupingItem
        where TItemFactory : IGroupingItemFactory<TKey, TSource, TItem>
        where TContinuation : struct, IStateMachine<ListChange<TResult>>
    {
        private TContinuation _continuation;
        private TItemFactory _itemFactory;

        private readonly List<TItem> _items = new();
        private readonly List<Grouping<TKey, TSource>?> _groupings = new();

        private readonly IEqualityComparer<TKey>? _keyComparer;
        private readonly Func<TKey, ObserverBuilder<ListChange<TSource>>, TResult> _resultSelector;

        public GroupByStateMachine(
            in TContinuation continuation,
            in TItemFactory itemFactory,
            IEqualityComparer<TKey>? keyComparer,
            Func<TKey, ObserverBuilder<ListChange<TSource>>, TResult> resultSelector)
        {
            _continuation = continuation;
            _itemFactory = itemFactory;
            _keyComparer = keyComparer;
            _resultSelector = resultSelector;
        }

        public StateMachineBox Box =>
            _continuation.Box;

        public StateMachine<ListChange<TSource>> Reference =>
            StateMachine<ListChange<TSource>>.Create(ref this);

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
            switch (value.Action)
            {
                case ListChangeAction.RemoveAll:
                    {
                        _itemFactory.DisposeAll(_items);
                        _items.Clear();

                        _groupings.Clear();
                        _continuation.OnNext(ListChange.RemoveAll<TResult>());

                        break;
                    }
                case ListChangeAction.Remove:
                    {
                        var index = value.OldIndex;
                        var item = _items[index];

                        RemoveItemFromGroup(item);

                        _items.RemoveAt(index);
                        _itemFactory.Dispose(item);
                        _itemFactory.SetOriginalIndexes(
                            items: CollectionsMarshal.AsSpan(_items).Slice(index),
                            indexChange: -1);

                        break;
                    }
                case ListChangeAction.Insert:
                    {
                        _itemFactory.Create(value.NewIndex, value.NewItem, ref this, replacement: false);
                        break;
                    }
                case ListChangeAction.Replace:
                    {
                        _itemFactory.Create(value.NewIndex, value.NewItem, ref this, replacement: true);
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

                        _itemFactory.SetOriginalIndexes(items, indexChange);
                        _itemFactory.SetOriginalIndex(item, newIndex);

                        break;
                    }
            }
        }


        public void AddItemDeferred(int index, TItem item) =>
            _items.Insert(index, item);

        public void AddItem(int index, TItem item, TSource source, TKey key)
        {
            var (grouping, groupingIndex) = GetGrouping(key);

            item.GroupingIndex = groupingIndex;
            item.GroupingItemIndex = grouping.Add(source);

            _items.Insert(index, item);
            _itemFactory.SetOriginalIndexes(
                items: CollectionsMarshal.AsSpan(_items).Slice(index),
                indexChange: 1);
        }

        public void UpdateItem(int index, TItem item, TSource source, TKey key)
        {
            var (grouping, groupingIndex) = GetGrouping(key);

            if (groupingIndex != item.GroupingIndex)
            {
                RemoveItemFromGroup(item);

                item.GroupingIndex = groupingIndex;
                item.GroupingItemIndex = grouping.Add(source);

                if (typeof(TItem).IsValueType)
                    _items[index] = item;
            }
        }

        public void ReplaceItem(int index, TItem item, TSource source, TKey key)
        {
            var oldItem = _items[index];
            var (grouping, groupingIndex) = GetGrouping(key);

            if (groupingIndex == oldItem.GroupingIndex)
            {
                item.GroupingIndex = oldItem.GroupingIndex;
                item.GroupingItemIndex = oldItem.GroupingItemIndex;

                _itemFactory.Dispose(oldItem);
                _items[index] = item;

                grouping.Replace(item.GroupingIndex, source);
            }
            else
            {
                RemoveItemFromGroup(oldItem);

                item.GroupingIndex = groupingIndex;
                item.GroupingItemIndex = grouping.Add(source);

                _items[index] = item;
            }
        }

        private void RemoveItemFromGroup(TItem item)
        {
            var grouping = _groupings[item.GroupingIndex];

            Debug.Assert(grouping is { });
            grouping.Remove(item.GroupingItemIndex);

            if (grouping.IsEmpty)
            {
                _groupings[item.GroupingIndex] = null;
                _continuation.OnNext(ListChange.Remove<TResult>(item.GroupingIndex));
            }

            foreach (ref var candidate in CollectionsMarshal.AsSpan(_items))
            {
                if (candidate.GroupingIndex == item.GroupingIndex)
                    candidate.GroupingItemIndex -= 1;
            }
        }

        private (Grouping<TKey, TSource>, int) GetGrouping(TKey key)
        {
            var hash = key is null ? 0 : _keyComparer?.GetHashCode(key) ?? EqualityComparer<TKey>.Default.GetHashCode(key);

            var freeIndex = -1;
            var currentIndex = 0;

            var comparer = _keyComparer;
            if (comparer is null && typeof(TKey).IsValueType)
            {
                foreach (var grouping in _groupings)
                {
                    if (grouping is null)
                        freeIndex = currentIndex;
                    else
                    if (grouping.KeyHash == hash && EqualityComparer<TKey>.Default.Equals(grouping.Key, key))
                        return (grouping, currentIndex);

                    currentIndex += 1;
                }
            }
            else
            {
                Debug.Assert(comparer is { });

                foreach (var grouping in _groupings)
                {
                    if (grouping is null)
                        freeIndex = currentIndex;
                    else
                    if (grouping.KeyHash == hash && comparer.Equals(grouping.Key, key))
                        return (grouping, currentIndex);

                    currentIndex += 1;
                }
            }

            {
                var grouping = new Grouping<TKey, TSource>
                {
                    Key = key,
                    KeyHash = hash
                };

                if (freeIndex == -1)
                {
                    freeIndex = _groupings.Count;
                    _groupings.Add(grouping);
                }
                else
                {
                    _groupings[freeIndex] = grouping;
                }

                _continuation.OnNext(ListChange.Insert(freeIndex, _resultSelector(key, grouping.ToBuilder())));

                return (grouping, freeIndex);
            }
        }
    }

    private sealed class Grouping<TKey, TElement> : IObservable<ListChange<TElement>>, IDisposable
    {
        private IObserver<ListChange<TElement>>? _items;
        private int _itemCount;

        public bool IsEmpty => _itemCount == 0;

        public required int KeyHash { get; init; }
        public required TKey Key { get; init; }

        public IDisposable Subscribe(IObserver<ListChange<TElement>> observer)
        {
            _items = _items is null ? observer : throw new InvalidOperationException();

            return this;
        }

        public void Dispose() =>
            _items = null;

        public int Add(TElement item)
        {
            var index = _itemCount;

            _items?.OnNext(ListChange.Insert(index, item));
            _itemCount += 1;

            return index;
        }

        public void Remove(int index)
        {
            _items?.OnNext(ListChange.Remove<TElement>(index));
            _itemCount -= 1;
        }

        public void Replace(int index, TElement item) =>
            _items?.OnNext(ListChange.Replace(index, item));
    }

    private interface IGroupByStateMachine<TKey, TSource, TItem> : IStateMachine<ListChange<TSource>>
    {
        void AddItemDeferred(int index, TItem item);
        void AddItem(int index, TItem item, TSource source, TKey key);
        void UpdateItem(int index, TItem item, TSource source, TKey key);
        void ReplaceItem(int index, TItem item, TSource source, TKey key);
    }

    private interface IGroupingItem
    {
        int GroupingIndex { get; set; }
        int GroupingItemIndex { get; set; }
    }

    private interface IGroupingItemFactory<TKey, TSource, TItem>
        where TItem : IGroupingItem
    {
        void Create<TGroupBy>(int sourceIndex, TSource source, ref TGroupBy groupBy, bool replacement)
            where TGroupBy : struct, IGroupByStateMachine<TKey, TSource, TItem>;

        void Dispose(TItem item);
        void DisposeAll(List<TItem> items);

        void SetOriginalIndex(TItem item, int index);
        void SetOriginalIndexes(ReadOnlySpan<TItem> items, int indexChange);
    }

    private struct GroupingStaticItem : IGroupingItem
    {
        public int GroupingIndex { get; set; }
        public int GroupingItemIndex { get; set; }

        public readonly struct Factory<TKey, TSource> : IGroupingItemFactory<TKey, TSource, GroupingStaticItem>
        {
            public required Func<TSource, TKey> KeySelector { get; init; }

            public void Create<TGroupBy>(int index, TSource source, ref TGroupBy groupBy, bool replacement)
                where TGroupBy : struct, IGroupByStateMachine<TKey, TSource, GroupingStaticItem>
            {
                var key = KeySelector(source);
                var item = new GroupingStaticItem();

                if (replacement)
                    groupBy.ReplaceItem(index, item, source, key);
                else
                    groupBy.AddItem(index, item, source, key);
            }

            public void Dispose(GroupingStaticItem item) { }
            public void DisposeAll(List<GroupingStaticItem> items) { }

            public void SetOriginalIndex(GroupingStaticItem item, int index) { }
            public void SetOriginalIndexes(ReadOnlySpan<GroupingStaticItem> items, int indexChange) { }
        }
    }

    private sealed class GroupingDynamicItem<TSource> : IGroupingItem
    {
        public int GroupingIndex { get; set; }
        public int GroupingItemIndex { get; set; }

        private readonly TSource _source;
        private int _sourceIndex;

        private IDisposable? _keyChanged;

        private GroupingDynamicItem(int sourceIndex, TSource source, bool replacement)
        {
            _source = source;
            _sourceIndex = sourceIndex;

            GroupingIndex = -1;
            GroupingItemIndex = replacement ? 0 : -1;
        }

        public readonly struct Factory<TKey> : IGroupingItemFactory<TKey, TSource, GroupingDynamicItem<TSource>>
        {
            public required Func<TSource, ObserverBuilder<TKey>> KeySelector { get; init; }

            public void Create<TGroupBy>(int index, TSource source, ref TGroupBy groupBy, bool replacement)
                where TGroupBy : struct, IGroupByStateMachine<TKey, TSource, GroupingDynamicItem<TSource>>
            {
                var item = new GroupingDynamicItem<TSource>(index, source, replacement);

                item._keyChanged = KeySelector(source)
                    .ContinueWith<StateMachineFactory<TKey, TGroupBy>, object>(
                        new() { Item = item, GroupBy = new StateMachineReference<ListChange<TSource>, TGroupBy>(ref groupBy) })
                    .Subscribe();

                if (item.GroupingIndex == -1)
                {
                    // The selector has an async code inside which hasn't finished yet.
                    groupBy.AddItemDeferred(index, item);
                }
            }

            public void Dispose(GroupingDynamicItem<TSource> item)
            {
                item._sourceIndex = -1;

                item._keyChanged?.Dispose();
                item._keyChanged = null;
            }

            public void DisposeAll(List<GroupingDynamicItem<TSource>> items)
            {
                foreach (var item in items)
                    Dispose(item);
            }

            public void SetOriginalIndex(GroupingDynamicItem<TSource> item, int index) =>
                item._sourceIndex = index;

            public void SetOriginalIndexes(ReadOnlySpan<GroupingDynamicItem<TSource>> items, int indexChange)
            {
                foreach (var item in items)
                    item._sourceIndex += indexChange;
            }
        }

        private readonly struct StateMachineFactory<TKey, TGroupBy> : IStateMachineFactory<TKey, object>
            where TGroupBy : struct, IGroupByStateMachine<TKey, TSource, GroupingDynamicItem<TSource>>
        {
            public required readonly GroupingDynamicItem<TSource> Item { get; init; }
            public required readonly StateMachineReference<ListChange<TSource>, TGroupBy> GroupBy { get; init; }

            public void Create<TContinuation>(in TContinuation continuation, ObserverStateMachine<TKey> source)
                where TContinuation : struct, IStateMachine<object>
            {
                source.ContinueWith<StateMachine<TKey, TGroupBy, TContinuation>>(
                    new(continuation, Item, GroupBy));
            }
        }

        private struct StateMachine<TKey, TGroupBy, TContinution> : IStateMachine<TKey>
            where TGroupBy : struct, IGroupByStateMachine<TKey, TSource, GroupingDynamicItem<TSource>>
            where TContinution : struct, IStateMachine<object>
        {
            private TContinution _continuation;

            private readonly GroupingDynamicItem<TSource> _item;
            private readonly StateMachineReference<ListChange<TSource>, TGroupBy> _groupBy;

            public StateMachine(
                in TContinution continution,
                GroupingDynamicItem<TSource> item,
                StateMachineReference<ListChange<TSource>, TGroupBy> groupBy)
            {
                _continuation = continution;

                _item = item;
                _groupBy = groupBy;
            }

            public StateMachineBox Box =>
                _continuation.Box;

            public StateMachine<TKey> Reference =>
                StateMachines.StateMachine<TKey>.Create(ref this);

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

            public void OnNext(TKey value)
            {
                // If there's no key changed subscription then it might be possible
                // that the item was disposed while waiting to be processed. So,
                // an additional check is required to see if the item was used.
                if (_item._sourceIndex == -1)
                    return;

                if (_item._keyChanged is { })
                {
                    if (_item.GroupingItemIndex != -1)
                        _groupBy.Target.UpdateItem(_item._sourceIndex, _item, _item._source, value);
                    else
                        _groupBy.Target.ReplaceItem(_item._sourceIndex, _item, _item._source, value);
                }
                else
                {
                    if (_item.GroupingItemIndex == -1)
                        _groupBy.Target.AddItem(_item._sourceIndex, _item, _item._source, value);
                    else
                        _groupBy.Target.ReplaceItem(_item._sourceIndex, _item, _item._source, value);
                }
            }
        }
    }
}