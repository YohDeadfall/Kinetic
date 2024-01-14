using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Kinetic.Linq.StateMachines;

namespace Kinetic.Linq;

public static partial class ObservableView
{
    public static ObserverBuilder<ListChange<T>> OrderBy<T, TKey>(this ObserverBuilder<ListChange<T>> source, Func<T, TKey> keySelector, IComparer<TKey>? keyComparer = null) =>
        source.ContinueWith<OrderByCore<T, TKey>.StateMachineFactory, ListChange<T>>(new(keySelector, keyComparer));

    public static ObserverBuilder<ListChange<T>> OrderBy<T, TKey>(this ReadOnlyObservableList<T> source, Func<T, TKey> keySelector, IComparer<TKey>? keyComparer = null) =>
        source.Changed.ToBuilder().OrderBy(keySelector, keyComparer);

    public static ObserverBuilder<ListChange<T>> OrderBy<T, TKey>(this ObserverBuilder<ListChange<T>> source, ObserverBuilderFactory<T, TKey> keySelector, IComparer<TKey>? keyComparer = null) =>
        source.ContinueWith<OrderByCore<T, TKey>.StateMachineFactory, ListChange<T>>(new(keySelector, keyComparer));

    public static ObserverBuilder<ListChange<T>> OrderBy<T, TKey>(this ObserverBuilder<ListChange<T>> source, Func<T, Property<TKey>> keySelector) =>
        source.OrderBy(item => keySelector(item).Changed.ToBuilder(), keyComparer: null);

    public static ObserverBuilder<ListChange<T>> OrderBy<T, TKey>(this ObserverBuilder<ListChange<T>> source, Func<T, Property<TKey>> keySelector, IComparer<TKey>? keyComparer = null) =>
        source.OrderBy(item => keySelector(item).Changed.ToBuilder(), keyComparer);

    public static ObserverBuilder<ListChange<T>> OrderBy<T, TKey>(this ObserverBuilder<ListChange<T>> source, Func<T, ReadOnlyProperty<TKey>> keySelector, IComparer<TKey>? keyComparer = null) =>
        source.OrderBy(item => keySelector(item).Changed.ToBuilder(), keyComparer);

    public static ObserverBuilder<ListChange<T>> OrderBy<T, TKey>(this ReadOnlyObservableList<T> source, ObserverBuilderFactory<T, TKey> keySelector, IComparer<TKey>? keyComparer = null) =>
        source.Changed.ToBuilder().OrderBy(keySelector, keyComparer);

    public static ObserverBuilder<ListChange<T>> OrderBy<T, TKey>(this ReadOnlyObservableList<T> source, Func<T, Property<TKey>> keySelector) =>
        source.OrderBy(item => keySelector(item).Changed.ToBuilder(), keyComparer: null);

    public static ObserverBuilder<ListChange<T>> OrderBy<T, TKey>(this ReadOnlyObservableList<T> source, Func<T, Property<TKey>> keySelector, IComparer<TKey>? keyComparer = null) =>
        source.OrderBy(item => keySelector(item).Changed.ToBuilder(), keyComparer);

    public static ObserverBuilder<ListChange<T>> OrderBy<T, TKey>(this ReadOnlyObservableList<T> source, Func<T, ReadOnlyProperty<TKey>> keySelector, IComparer<TKey>? keyComparer = null) =>
        source.OrderBy(item => keySelector(item).Changed.ToBuilder(), keyComparer);

    private static class OrderByCore<T, TKey>
    {
        // A single element value tuple is used here to solve a conflict between interfaces.
        private interface IStateMachine<TItem> :
            StateMachines.IStateMachine<ListChange<T>>,
            StateMachines.IStateMachine<ValueTuple<TItem>>,
            StateMachines.IStateMachine<IComparer<TKey>?>
            where TItem : IItem<TItem>
        {
        }

        private interface IItem<TSelf> : IComparable<TSelf>, IDisposable
            where TSelf : IItem<TSelf>
        {
            public int OriginalIndex { get; set; }

            public TKey? Key { get; }
            public T Value { get; }

            public abstract static TSelf Create(TKey? key, T value);

            public void Initialize(IDisposable? subscription);
        }

        private interface IKeySelector<TItem>
            where TItem : IItem<TItem>
        {
            public (TItem, IDisposable?) CreateItem<TStateMachine>(int index, T value, ref TStateMachine stateMachine)
                where TStateMachine : struct, IStateMachine<TItem>;
        }

        private interface IKeyComparer<TItem> : IDisposable
            where TItem : IItem<TItem>
        {
            public ItemComparer<TItem>? ItemComparer { get; set; }

            public void Initialize<TStateMachine>(ref TStateMachine stateMachine)
                where TStateMachine : struct, IStateMachine<TItem>;
        }

        public readonly struct StateMachineFactory : IStateMachineFactory<ListChange<T>, ListChange<T>>
        {
            private readonly object _keySelector;
            private readonly object? _keyComparer;

            public StateMachineFactory(Func<T, TKey> keySelector, IComparer<TKey>? keyComparer)
            {
                _keySelector = keySelector;
                _keyComparer = keyComparer;
            }

            public StateMachineFactory(Func<T, TKey> keySelector, IObservable<IComparer<TKey>?> keyComparer)
            {
                _keySelector = keySelector;
                _keyComparer = keyComparer;
            }

            public StateMachineFactory(ObserverBuilderFactory<T, TKey> keySelector, IComparer<TKey>? keyComparer)
            {
                _keySelector = keySelector;
                _keyComparer = keyComparer;
            }

            public StateMachineFactory(ObserverBuilderFactory<T, TKey> keySelector, IObservable<IComparer<TKey>?> keyComparer)
            {
                _keySelector = keySelector;
                _keyComparer = keyComparer;
            }

            public void Create<TContinuation>(in TContinuation continuation, ObserverStateMachine<ListChange<T>> source)
                where TContinuation : struct, StateMachines.IStateMachine<ListChange<T>>
            {
                if (_keyComparer is IObservable<IComparer<TKey>?> dynamicComparer)
                {
                    if (_keySelector is ObserverBuilderFactory<T, TKey> dynamicSelector)
                    {
                        source.ContinueWith<StateMachine<DynamicItem, DynamicKeySelector, DynamicKeyComparer<DynamicItem>, TContinuation>>(
                            new(continuation, new(dynamicSelector), new(dynamicComparer)));
                    }
                    else
                    {
                        var staticSelector = (Func<T, TKey>) _keySelector;
                        source.ContinueWith<StateMachine<DynamicItem, StaticKeySelector<DynamicItem>, DynamicKeyComparer<DynamicItem>, TContinuation>>(
                            new(continuation, new(staticSelector), new(dynamicComparer)));
                    }
                }
                else
                {
                    var staticComparer = (IComparer<TKey>?) _keyComparer;
                    if (_keySelector is Func<T, TKey> staticSelector)
                    {
                        source.ContinueWith<StateMachine<StaticItem, StaticKeySelector<StaticItem>, StaticKeyComparer<StaticItem>, TContinuation>>(
                            new(continuation, new(staticSelector), new(staticComparer)));
                    }
                    else
                    {
                        var dynamicSelector = (ObserverBuilderFactory<T, TKey>) _keySelector;
                        source.ContinueWith<StateMachine<DynamicItem, DynamicKeySelector, StaticKeyComparer<DynamicItem>, TContinuation>>(
                            new(continuation, new(dynamicSelector), new(staticComparer)));
                    }
                }
            }
        }

        private struct StateMachine<TItem, TKeySelector, TKeyComparer, TContinuation> :
            IStateMachine<TItem>
            where TItem : IItem<TItem>
            where TKeySelector : struct, IKeySelector<TItem>
            where TKeyComparer : struct, IKeyComparer<TItem>
            where TContinuation : struct, StateMachines.IStateMachine<ListChange<T>>
        {
            private readonly List<TItem> _items;
            private readonly List<int> _indexes;
            private TContinuation _continuation;
            private TKeySelector _keySelector;
            private TKeyComparer _keyComparer;

            public StateMachine(in TContinuation continuation, in TKeySelector keySelector, in TKeyComparer keyComparer)
            {
                _continuation = continuation;
                _keySelector = keySelector;
                _keyComparer = keyComparer;

                _items = new List<TItem>();
                _indexes = new List<int>();
            }

            public StateMachineBox Box =>
                _continuation.Box;

            public void Initialize(StateMachineBox box)
            {
                _continuation.Initialize(box);
                _keyComparer.Initialize(ref this);
            }

            public void Dispose()
            {
                foreach (var item in _items)
                    item.Dispose();

                _keyComparer.Dispose();
                _continuation.Dispose();
            }

            public void OnCompleted() =>
                _continuation.OnCompleted();

            public void OnError(Exception error) =>
                _continuation.OnError(error);

            public void OnNext(ListChange<T> value)
            {
                switch (value.Action)
                {
                    case ListChangeAction.RemoveAll:
                        {
                            foreach (var item in _items)
                                item.Dispose();

                            _indexes.Clear();
                            _items.Clear();

                            _continuation.OnNext(value);

                            break;
                        }
                    case ListChangeAction.Remove
                    when value.OldIndex is var originalIndex:
                        {
                            var index = _indexes[originalIndex];
                            var item = _items[index];

                            item.Dispose();

                            _indexes.RemoveAt(originalIndex);
                            _items.RemoveAt(index);

                            var indexes = CollectionsMarshal.AsSpan(_indexes);
                            foreach (ref var current in indexes)
                            {
                                if (current > index)
                                    current -= 1;

                                var offset = Unsafe.ByteOffset(ref indexes[0], ref current).ToInt32() / Unsafe.SizeOf<int>();
                                if (offset > originalIndex)
                                    _items[current].OriginalIndex = offset;
                            }

                            _continuation.OnNext(
                                ListChange.Remove<T>(index));

                            break;
                        }
                    case ListChangeAction.Insert
                    when value.NewIndex is var originalIndex:
                        {
                            var (item, subscription) = _keySelector.CreateItem(originalIndex, value.NewItem, ref this);
                            var index = _items.BinarySearch(item, _keyComparer.ItemComparer);

                            if (index < 0)
                                index = ~index;

                            var indexes = CollectionsMarshal.AsSpan(_indexes);
                            foreach (ref var current in indexes)
                            {
                                if (current >= index)
                                    current += 1;

                                var offset = Unsafe.ByteOffset(ref indexes[0], ref current).ToInt32() / Unsafe.SizeOf<int>();
                                if (offset > originalIndex)
                                    _items[current].OriginalIndex = offset;
                            }

                            _indexes.Insert(originalIndex, index);
                            _items.Insert(index, item);

                            _continuation.OnNext(
                                ListChange.Insert(index, value.NewItem));

                            item.Initialize(subscription);
                            break;
                        }
                    case ListChangeAction.Replace
                    when value.NewIndex is var originalIndex:
                        {
                            var oldIndex = _indexes[originalIndex];
                            var oldItem = _items[oldIndex];

                            oldItem.Dispose();

                            var (newItem, subscription) = _keySelector.CreateItem(originalIndex, value.NewItem, ref this);
                            var newIndex = _items.BinarySearch(newItem, _keyComparer.ItemComparer);

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

                                // Could be optimized using arrow manipulations
                                _items.RemoveAt(oldIndex);
                                _items.Insert(newIndex, newItem);

                                _continuation.OnNext(ListChange.Remove<T>(oldIndex));
                                _continuation.OnNext(ListChange.Insert(newIndex, value.NewItem));
                            }

                            newItem.Initialize(subscription);
                            break;
                        }
                    case ListChangeAction.Move:
                        {
                            var index = _indexes[value.OldIndex];

                            _indexes.RemoveAt(value.OldIndex);
                            _indexes.Insert(value.NewIndex, index);

                            _items[index].OriginalIndex = value.NewIndex;

                            break;
                        }
                }
            }

            public void OnNext(ValueTuple<TItem> value)
            {
                Debug.Assert(typeof(TItem) == typeof(DynamicItem));

                var item = value.Item1;
                // Old index search is based on reference equality,
                // while the new index is key comparison based.
                var oldIndex = _items.IndexOf(item);
                var newIndex = _items.BinarySearch(item, _keyComparer.ItemComparer);

                if (newIndex < 0)
                    newIndex = ~newIndex;

                if (oldIndex != newIndex)
                {
                    newIndex = UpdateIndexes(oldIndex, newIndex);

                    _items.RemoveAt(oldIndex);
                    _items.Insert(newIndex, item);

                    _indexes[item.OriginalIndex] = newIndex;
                    _continuation.OnNext(ListChange.Move<T>(oldIndex, newIndex));
                }
            }

            public void OnNext(IComparer<TKey>? value)
            {
                Debug.Assert(typeof(TItem) == typeof(DynamicItem));

                _keyComparer.ItemComparer = ItemComparer<TItem>.Create(value);
                _items.Sort(_keyComparer.ItemComparer);

                for (var newIndex = 0; newIndex < _items.Count; newIndex += 1)
                {
                    var item = _items[newIndex];
                    var oldIndex = _indexes[item.OriginalIndex];

                    if (oldIndex != newIndex)
                    {
                        _indexes[item.OriginalIndex] = newIndex;
                        _continuation.OnNext(ListChange.Move<T>(oldIndex, newIndex));
                    }
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
                        if (current > newIndex && current < oldIndex)
                            current += 1;
                    }

                    return newIndex;
                }
            }
        }

        private sealed class ItemComparer<TItem> : IComparer<TItem>
            where TItem : IItem<TItem>
        {
            private readonly IComparer<TKey> _keyComparer;

            public static ItemComparer<TItem>? Create(IComparer<TKey>? keyComparer) =>
                keyComparer is { } ? new(keyComparer) : null;

            public ItemComparer(IComparer<TKey> keyComparer) =>
                _keyComparer = keyComparer;

            public int Compare(TItem? left, TItem? rigth) =>
                _keyComparer.Compare(left!.Key, rigth!.Key);
        }

        private readonly struct StaticItem : IItem<StaticItem>
        {
            public int OriginalIndex
            {
                get => throw new NotSupportedException("Must not be used.");
                set { }
            }

            public TKey? Key { get; }
            public T Value => throw new NotSupportedException("Must not be used.");

            public StaticItem(TKey? key) =>
                Key = key;

            public static StaticItem Create(TKey? key, T value) =>
                new(key);

            public int CompareTo(StaticItem other) =>
                Comparer<TKey>.Default.Compare(Key, other.Key);

            public void Initialize(IDisposable? subscription) =>
                Debug.Assert(subscription is null);

            public void Dispose() { }
        }

        private sealed class DynamicItem : IItem<DynamicItem>
        {
            private IDisposable? _subscription;

            public int OriginalIndex { get; set; }

            public TKey? Key { get; private set; }
            public T Value { get; }

            public DynamicItem(TKey? key, T value)
            {
                Key = key;
                Value = value;
            }

            public static DynamicItem Create(TKey? key, T value) =>
                new(key, value);

            public int CompareTo(DynamicItem? other) =>
                Comparer<TKey>.Default.Compare(Key, other!.Key);

            public void Initialize(IDisposable? subscription) =>
                _subscription = subscription;

            public void Dispose() =>
                _subscription?.Dispose();

            public struct StateMachine<TContinuation> : StateMachines.IStateMachine<TKey>
                where TContinuation : struct, StateMachines.IStateMachine<ValueTuple<OrderByCore<T, TKey>.DynamicItem>>
            {
                private readonly DynamicItem _item;
                private TContinuation _continuation;

                public StateMachine(DynamicItem item, in TContinuation continuation)
                {
                    _item = item;
                    _continuation = continuation;
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

                public void OnNext(TKey value)
                {
                    _item.Key = value;

                    if (_item._subscription is { })
                        _continuation.OnNext(ValueTuple.Create(_item));
                }
            }

            public readonly struct StateMachineFactory : IStateMachineFactory<TKey, ValueTuple<DynamicItem>>
            {
                private readonly DynamicItem _item;

                public StateMachineFactory(DynamicItem item) =>
                    _item = item;

                public void Create<TContinuation>(in TContinuation continuation, ObserverStateMachine<TKey> source)
                    where TContinuation : struct, StateMachines.IStateMachine<ValueTuple<OrderByCore<T, TKey>.DynamicItem>>
                {
                    source.ContinueWith(new StateMachine<TContinuation>(_item, continuation));
                }
            }
        }

        private readonly struct StaticKeySelector<TItem> : IKeySelector<TItem>
            where TItem : IItem<TItem>
        {
            private readonly Func<T, TKey> _keySelector;

            public StaticKeySelector(Func<T, TKey> keySelector) =>
                _keySelector = keySelector;

            public (TItem, IDisposable?) CreateItem<TStateMachine>(int index, T value, ref TStateMachine stateMachinen)
                where TStateMachine : struct, IStateMachine<TItem>
            {
                var key = _keySelector(value);
                var item = TItem.Create(key, value);

                item.OriginalIndex = index;
                return (item, null);
            }
        }

        private readonly struct DynamicKeySelector : IKeySelector<DynamicItem>
        {
            private readonly ObserverBuilderFactory<T, TKey> _keySelector;

            public DynamicKeySelector(ObserverBuilderFactory<T, TKey> keySelector) =>
                _keySelector = keySelector;

            public (DynamicItem, IDisposable?) CreateItem<TStateMachine>(int index, T value, ref TStateMachine stateMachine)
                where TStateMachine : struct, IStateMachine<DynamicItem>
            {
                var item = DynamicItem.Create(default, value);
                var subscription = _keySelector
                    .Invoke(value)
                    .ContinueWith<DynamicItem.StateMachineFactory, ValueTuple<DynamicItem>>(new DynamicItem.StateMachineFactory(item))
                    .Subscribe(ref stateMachine);

                item.OriginalIndex = index;
                return (item, subscription);
            }
        }

        private struct StaticKeyComparer<TItem> : IKeyComparer<TItem>
            where TItem : IItem<TItem>
        {
            public ItemComparer<TItem>? ItemComparer { get; set; }

            public StaticKeyComparer(IComparer<TKey>? keyComparer) =>
                ItemComparer = ItemComparer<TItem>.Create(keyComparer);

            public void Initialize<TStateMachine>(ref TStateMachine stateMachine)
                where TStateMachine : struct, IStateMachine<TItem>
            { }

            public void Dispose() { }
        }

        private struct DynamicKeyComparer<TItem> : IKeyComparer<TItem>
            where TItem : IItem<TItem>
        {
            private readonly IObservable<IComparer<TKey>?> _observable;
            private IDisposable? _subscription;

            public ItemComparer<TItem>? ItemComparer { get; set; }

            public DynamicKeyComparer(IObservable<IComparer<TKey>?> observable) =>
                _observable = observable;

            public void Initialize<TStateMachine>(ref TStateMachine stateMachine)
                where TStateMachine : struct, IStateMachine<TItem>
            {
                Debug.Assert(_subscription is null);

                _subscription = _observable.Subscribe(ref stateMachine);
            }

            public void Dispose() =>
                _subscription?.Dispose();
        }
    }
}