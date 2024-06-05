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
            new() { KeySelector = keySelector, ResultSelector = resultSelector });
    }

    public static ObserverBuilder<ListChange<TResult>> GroupBy<TSource, TKey, TResult>(
        this ObserverBuilder<ListChange<TSource>> source,
        Func<TSource, TKey> keySelector,
        Func<TKey, ObserverBuilder<ListChange<TSource>>, TResult> resultSelector,
        IEqualityComparer<TKey> keyComparer)
    {
        return source.ContinueWith<GroupByStateMachineFactory<TKey, TSource, TResult>, ListChange<TResult>>(
            new() { KeyComparer = keyComparer, KeySelector = keySelector, ResultSelector = resultSelector });
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

    private readonly struct GroupByStateMachineFactory<TKey, TSource, TResult> : IStateMachineFactory<ListChange<TSource>, ListChange<TResult>>
    {
        public IEqualityComparer<TKey>? KeyComparer { get; init; }
        public required Func<TSource, TKey> KeySelector { get; init; }
        public required Func<TKey, ObserverBuilder<ListChange<TSource>>, TResult> ResultSelector { get; init; }

        public void Create<TContinuation>(in TContinuation continuation, ObserverStateMachine<ListChange<TSource>> source)
            where TContinuation : struct, IStateMachine<ListChange<TResult>> =>
            source.ContinueWith<GroupByStateMachine<TKey, TSource, TResult, TContinuation>>(
                new(continuation, KeyComparer, KeySelector, ResultSelector));
    }

    private struct GroupByStateMachine<TKey, TSource, TResult, TContinuation> : IStateMachine<ListChange<TSource>>
        where TContinuation : struct, IStateMachine<ListChange<TResult>>
    {
        private TContinuation _continuation;
        private readonly IEqualityComparer<TKey>? _keyComparer;
        private readonly Func<TSource, TKey> _keySelector;
        private readonly Func<TKey, ObserverBuilder<ListChange<TSource>>, TResult> _resultSelector;

        private readonly List<(int GroupingIndex, int ItemIndex)> _indices = new();
        private readonly List<Grouping<TKey, TSource>?> _groupings = new();

        public GroupByStateMachine(
            in TContinuation continuation,
            IEqualityComparer<TKey>? keyComparer,
            Func<TSource, TKey> keySelector,
            Func<TKey, ObserverBuilder<ListChange<TSource>>, TResult> resultSelector)
        {
            _continuation = continuation;
            _keyComparer = typeof(TKey).IsValueType
                ? keyComparer is { } && keyComparer == EqualityComparer<TKey>.Default ? null : keyComparer
                : keyComparer ?? EqualityComparer<TKey>.Default;
            _keySelector = keySelector;
            _resultSelector = resultSelector;
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
            switch (value.Action)
            {
                case ListChangeAction.RemoveAll:
                    {
                        _groupings.Clear();
                        _indices.Clear();

                        _continuation.OnNext(ListChange.RemoveAll<TResult>());

                        break;
                    }
                case ListChangeAction.Remove:
                    {
                        var indices = _indices[value.OldIndex];
                        var grouping = _groupings[indices.GroupingIndex];

                        Debug.Assert(grouping is { });
                        RemoveItem(grouping, indices.GroupingIndex, indices.ItemIndex);

                        foreach (ref var candidate in CollectionsMarshal.AsSpan(_indices))
                        {
                            if (candidate.ItemIndex > indices.ItemIndex)
                                candidate.ItemIndex -= 1;
                        }

                        break;
                    }
                case ListChangeAction.Insert:
                    {
                        var (item, itemIndex) = (value.NewItem, value.NewIndex);
                        var (grouping, groupingIndex) = GetGrouping(item);

                        _indices.Insert(itemIndex, (groupingIndex, grouping.Add(item)));

                        break;
                    }
                case ListChangeAction.Replace:
                    {
                        var item = value.NewItem;
                        var itemIndex = value.OldIndex;
                        var indices = _indices[itemIndex];
                        var oldGrouping = _groupings[indices.GroupingIndex];
                        var newGrouping = GetGrouping(item);

                        if (oldGrouping == newGrouping.Item1)
                        {
                            oldGrouping.Replace(indices.ItemIndex, item);
                        }
                        else
                        {
                            Debug.Assert(oldGrouping is { });
                            RemoveItem(oldGrouping, indices.GroupingIndex, indices.ItemIndex);

                            _indices[itemIndex] = (newGrouping.Item2, newGrouping.Item1.Add(item));
                        }

                        break;
                    }
            }
        }

        private (Grouping<TKey, TSource>, int) GetGrouping(TSource item)
        {
            var key = _keySelector(item);
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

                _continuation.OnNext(ListChange.Insert(currentIndex, _resultSelector(key, grouping.ToBuilder())));

                return (grouping, freeIndex);
            }
        }

        private void RemoveItem(Grouping<TKey, TSource> grouping, int groupingIndex, int itemIndex)
        {
            grouping.Remove(itemIndex);

            if (grouping.IsEmpty)
            {
                _groupings[groupingIndex] = null;
                _continuation.OnNext(ListChange.Remove<TResult>(groupingIndex));
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
            _items?.OnNext(
                ListChange.Replace(index, item));
    }
}