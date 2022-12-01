using System;
using System.Collections.Generic;
using Kinetic.Linq.StateMachines;

namespace Kinetic.Linq;

public static partial class ObservableView
{
    public static ObserverBuilder<ListChange<T>> OrderBy<T, TKey>(this ObserverBuilder<ListChange<T>> source, Func<T, TKey> keySelector, IComparer<TKey>? keyComparer = null) =>
        source.ContinueWith<OrderByStateMachineFactory<T, TKey>, ListChange<T>>(new(keySelector, keyComparer));

    public static ObserverBuilder<ListChange<T>> OrderBy<T, TKey>(this ReadOnlyObservableList<T> source, Func<T, TKey> keySelector, IComparer<TKey>? keyComparer = null) =>
        source.Changed.ToBuilder().OrderBy(keySelector, keyComparer);

    private readonly struct OrderByStateMachineFactory<T, TKey> : IObserverStateMachineFactory<ListChange<T>, ListChange<T>>
    {
        private readonly Func<T, TKey> _keySelector;
        private readonly IComparer<TKey>? _keyComparer;

        public OrderByStateMachineFactory(Func<T, TKey> keySelector, IComparer<TKey>? keyComparer)
        {
            _keySelector = keySelector;
            _keyComparer = keyComparer;
        }

        public void Create<TContinuation>(in TContinuation continuation, ObserverStateMachine<ListChange<T>> source)
            where TContinuation : struct, IObserverStateMachine<ListChange<T>> =>
            source.ContinueWith(new OrderByStateMachine<T, TKey, TContinuation>(continuation, _keySelector, _keyComparer));
    }

    private struct OrderByStateMachine<T, TKey, TContinuation> : IObserverStateMachine<ListChange<T>>
        where TContinuation : struct, IObserverStateMachine<ListChange<T>>
    {
        private TContinuation _continuation;
        private readonly IComparer<TKey>? _keyComparer;
        private readonly Func<T, TKey> _keySelector;
        private readonly List<int> _indexes;
        private readonly List<T> _items;
        private readonly List<TKey> _keys;

        public OrderByStateMachine(in TContinuation continuation, Func<T, TKey> keySelector, IComparer<TKey>? keyComparer)
        {
            _continuation = continuation;
            _keySelector = keySelector;
            _keyComparer = keyComparer;

            _indexes = new List<int>();
            _items = new List<T>();
            _keys = new List<TKey>();
        }

        public void Dispose() =>
            _continuation.Dispose();

        public void Initialize(ObserverStateMachineBox box) =>
            _continuation.Initialize(box);

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
                        _keys.Clear();
                        _items.Clear();
                        _indexes.Clear();

                        _continuation.OnNext(
                            ListChange.RemoveAll<T>());

                        break;
                    }
                case ListChangeAction.Remove:
                    {
                        var index = RemoveItem(value.OldIndex);

                        _continuation.OnNext(
                            ListChange.Remove<T>(index));

                        break;
                    }
                case ListChangeAction.Insert:
                    {
                        var item = value.NewItem;
                        var index = InsertItem(value.NewIndex, item);

                        _continuation.OnNext(
                            ListChange.Insert(index, item));

                        break;
                    }
                case ListChangeAction.Replace:
                    {
                        var oldIndex = RemoveItem(value.OldIndex);

                        var newItem = value.NewItem;
                        var newIndex = InsertItem(value.NewIndex, newItem);

                        if (oldIndex == newIndex)
                        {
                            _continuation.OnNext(
                                ListChange.Replace(oldIndex, newItem));
                        }
                        else
                        {
                            _continuation.OnNext(
                                ListChange.Remove<T>(oldIndex));
                            _continuation.OnNext(
                                ListChange.Insert(newIndex, newItem));
                        }

                        break;
                    }
            }
        }

        private int InsertItem(int index, T item)
        {
            var key = _keySelector(item);
            var adjustedIndex = _keys.BinarySearch(key, _keyComparer);

            if (adjustedIndex < 0)
            {
                adjustedIndex = ~adjustedIndex;
            }

            _keys.Insert(adjustedIndex, key);
            _items.Insert(adjustedIndex, item);
            _indexes.Insert(index, adjustedIndex);

            return adjustedIndex;
        }

        private int RemoveItem(int index)
        {
            var adjustedIndex = _indexes[index];

            _keys.RemoveAt(adjustedIndex);
            _items.RemoveAt(adjustedIndex);
            _indexes.RemoveAt(index);

            return adjustedIndex;
        }
    }
}