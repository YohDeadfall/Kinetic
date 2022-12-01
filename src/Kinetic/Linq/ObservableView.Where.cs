using System;
using System.Collections.Generic;
using Kinetic.Linq.StateMachines;

namespace Kinetic.Linq;

public static partial class ObservableView
{
    public static ObserverBuilder<ListChange<T>> Where<T>(this ObserverBuilder<ListChange<T>> source, ObserverBuilderFactory<T, bool> predicate) =>
        source.ContinueWith<WhereStateMachineFactory<T>, ListChange<T>>(new(predicate));

    public static ObserverBuilder<ListChange<T>> Where<T>(this ReadOnlyObservableList<T> source, ObserverBuilderFactory<T, bool> predicate) =>
        source.Changed.ToBuilder().Where(predicate);

    private struct WhereStateMachine<T, TContinuation> :
        IObserverStateMachine<ListChange<T>>,
        IObserverStateMachine<WhereStateMachineItem<T>>
        where TContinuation : struct, IObserverStateMachine<ListChange<T>>
    {
        private TContinuation _continuation;
        private ObserverBuilderFactory<T, bool> _predicate;
        private List<WhereStateMachineItem<T>> _items = new();
        private ObserverStateMachineBox? _box;

        public WhereStateMachine(in TContinuation continuation, ObserverBuilderFactory<T, bool> predicate)
        {
            _continuation = continuation;
            _predicate = predicate;
        }

        public void Dispose()
        {
            foreach (var item in _items)
                item.Dispose();
        }

        public void Initialize(ObserverStateMachineBox box) =>
            _box = box;

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

                        _items.Clear();
                        _continuation.OnNext(value);
                        break;
                    }
                case ListChangeAction.Remove:
                    {
                        var index = value.OldIndex;
                        var item = _items[index];

                        item.Dispose();
                        _items.RemoveAt(index);

                        if (item.Present)
                        {
                            _continuation.OnNext(
                                ListChange.Remove<T>(
                                    index: CountBefore(index)));
                        }

                        break;
                    }
                case ListChangeAction.Insert:
                    {
                        var item = new WhereStateMachineItem<T>(value.NewItem, value.NewIndex);
                        var subscription = _predicate(item.Item)
                            .ContinueWith<WherePredicateStateMachineFactory<T>, WhereStateMachineItem<T>>(new(item))
                            .Subscribe(this, _box!);

                        _items.Insert(item.Index, item);
                        item.Initialize(subscription);

                        if (item.Present)
                        {
                            _continuation.OnNext(
                                ListChange.Insert(
                                    index: CountBefore(item.Index),
                                    item.Item));
                        }

                        break;
                    }
                case ListChangeAction.Replace:
                    {
                        var index = value.OldIndex;
                        var oldItem = _items[index];

                        oldItem.Dispose();

                        var newItem = new WhereStateMachineItem<T>(value.NewItem, value.NewIndex);
                        var newSubscription = _predicate(newItem.Item)
                            .ContinueWith<WherePredicateStateMachineFactory<T>, WhereStateMachineItem<T>>(new(newItem))
                            .Subscribe(this, _box!);

                        _items[index] = newItem;
                        newItem.Initialize(newSubscription);

                        if (oldItem.Present)
                        {
                            if (newItem.Present)
                            {
                                _continuation.OnNext(
                                    ListChange.Replace(
                                        index: CountBefore(index),
                                        newItem.Item));
                            }
                            else
                            {
                                _continuation.OnNext(
                                    ListChange.Remove<T>(
                                        index: CountBefore(index)));
                            }
                        }
                        else
                        {
                            if (newItem.Present)
                            {
                                _continuation.OnNext(
                                    ListChange.Insert(
                                        index: CountBefore(index),
                                        newItem.Item));
                            }
                        }

                        break;
                    }
                case ListChangeAction.Move when
                    value.OldIndex is var oldIndex &&
                    value.NewIndex is var newIndex &&
                    newIndex != oldIndex:
                    {
                        var item = _items[oldIndex];

                        _items.RemoveAt(oldIndex);
                        _items.Insert(newIndex, item);

                        item.Index = newIndex;

                        if (item.Present)
                        {
                            var oldIndexTranslated = CountBefore(oldIndex);
                            var newIndexTranslated = CountBefore(newIndex);

                            if (newIndexTranslated > oldIndexTranslated)
                            {
                                newIndexTranslated -= 1;
                            }

                            _continuation.OnNext(
                                ListChange.Move(
                                    oldIndexTranslated,
                                    newIndexTranslated,
                                    item.Item));
                        }

                        break;
                    }
            }
        }

        public void OnNext(WhereStateMachineItem<T> value)
        {
            var index = CountBefore(value.Index);

            _continuation.OnNext(value.Present
                ? ListChange.Insert(index, value.Item)
                : ListChange.Remove<T>(index));
        }

        private int CountBefore(int index)
        {
            int count = 0;

            while (true)
            {
                index -= 1;

                if (index < 0)
                    break;

                if (_items[index].Present)
                    count += 1;
            }

            return count;
        }
    }

    private readonly struct WhereStateMachineFactory<T> : IObserverStateMachineFactory<ListChange<T>, ListChange<T>>
    {
        private readonly ObserverBuilderFactory<T, bool> _predicate;

        public WhereStateMachineFactory(ObserverBuilderFactory<T, bool> predicate) =>
            _predicate = predicate;

        public void Create<TContinuation>(in TContinuation continuation, ObserverStateMachine<ListChange<T>> source)
            where TContinuation : struct, IObserverStateMachine<ListChange<T>> =>
            source.ContinueWith(new WhereStateMachine<T, TContinuation>(continuation, _predicate));
    }

    private sealed class WhereStateMachineItem<T> : IDisposable
    {
        private IDisposable? _subscription;

        public T Item { get; }
        public int Index { get; set; }
        public bool Present { get; set; }
        public bool Initialized => _subscription is not null;

        public WhereStateMachineItem(T item, int index)
        {
            Item = item;
            Index = index;
        }

        public void Dispose() =>
            _subscription?.Dispose();

        public void Initialize(IDisposable subscription) =>
            _subscription = subscription;
    }

    private readonly struct WherePredicateStateMachineFactory<T> : IObserverStateMachineFactory<bool, WhereStateMachineItem<T>>
    {
        private readonly WhereStateMachineItem<T> _items;

        public WherePredicateStateMachineFactory(WhereStateMachineItem<T> item) =>
            _items = item;

        public void Create<TContinuation>(in TContinuation continuation, ObserverStateMachine<bool> source)
            where TContinuation : struct, IObserverStateMachine<WhereStateMachineItem<T>> =>
            source.ContinueWith(new WherePredicateStateMachine<T, TContinuation>(continuation, _items));
    }

    private struct WherePredicateStateMachine<T, TContinuation> : IObserverStateMachine<bool>
        where TContinuation : struct, IObserverStateMachine<WhereStateMachineItem<T>>
    {
        private TContinuation _continuation;
        private WhereStateMachineItem<T> _item;

        public WherePredicateStateMachine(in TContinuation continuation, WhereStateMachineItem<T> item)
        {
            _continuation = continuation;
            _item = item;
        }

        public void Dispose() =>
            _continuation.Dispose();

        public void Initialize(ObserverStateMachineBox box) { }

        public void OnCompleted() =>
            _continuation.OnCompleted();

        public void OnError(Exception error) =>
            _continuation.OnError(error);

        public void OnNext(bool value)
        {
            if (_item.Present == value)
                return;

            _item.Present = value;

            if (_item.Initialized)
                _continuation.OnNext(_item);
        }
    }
}