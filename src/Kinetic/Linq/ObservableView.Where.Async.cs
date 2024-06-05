using System;
using System.Collections.Generic;
using Kinetic.Linq.StateMachines;

namespace Kinetic.Linq;

public static partial class ObservableView
{
    public static ObserverBuilder<ListChange<T>> Where<T>(this ObserverBuilder<ListChange<T>> source, Func<T, ObserverBuilder<bool>> predicate) =>
        source.ContinueWith<WhereAsyncStateMachineFactory<T>, ListChange<T>>(new(predicate));

    public static ObserverBuilder<ListChange<T>> Where<T>(this ObserverBuilder<ListChange<T>> source, Func<T, Property<bool>> predicate) =>
        source.Where((item) => predicate(item).Changed.ToBuilder());

    public static ObserverBuilder<ListChange<T>> Where<T>(this ObserverBuilder<ListChange<T>> source, Func<T, ReadOnlyProperty<bool>> predicate) =>
        source.Where((item) => predicate(item).Changed.ToBuilder());

    public static ObserverBuilder<ListChange<T>> Where<T>(this ObserverBuilder<ListChange<T>> source, Func<T, IObservable<bool>> predicate) =>
        source.Where((item) => predicate(item).ToBuilder());

    public static ObserverBuilder<ListChange<T>> Where<T>(this ReadOnlyObservableList<T> source, Func<T, ObserverBuilder<bool>> predicate) =>
        source.Changed.ToBuilder().Where(predicate);

    public static ObserverBuilder<ListChange<T>> Where<T>(this ReadOnlyObservableList<T> source, Func<T, Property<bool>> predicate) =>
        source.Changed.ToBuilder().Where((item) => predicate(item).Changed.ToBuilder());

    public static ObserverBuilder<ListChange<T>> Where<T>(this ReadOnlyObservableList<T> source, Func<T, ReadOnlyProperty<bool>> predicate) =>
        source.Changed.ToBuilder().Where((item) => predicate(item).Changed.ToBuilder());

    public static ObserverBuilder<ListChange<T>> Where<T>(this ReadOnlyObservableList<T> source, Func<T, IObservable<bool>> predicate) =>
        source.Changed.ToBuilder().Where((item) => predicate(item).ToBuilder());

    private struct WhereAsyncStateMachine<T, TContinuation> :
        IStateMachine<ListChange<T>>,
        IStateMachine<ObservableViewItem<T>>
        where TContinuation : struct, IStateMachine<ListChange<T>>
    {
        private TContinuation _continuation;
        private Func<T, ObserverBuilder<bool>> _predicate;
        private List<ObservableViewItem<T>> _items = new();

        public WhereAsyncStateMachine(in TContinuation continuation, Func<T, ObserverBuilder<bool>> predicate)
        {
            _continuation = continuation;
            _predicate = predicate;
        }

        public StateMachineBox Box =>
            _continuation.Box;

        public void Initialize(StateMachineBox box) =>
            _continuation.Initialize(box);

        public void Dispose()
        {
            foreach (var item in _items)
                item.Dispose();

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
                        var item = new ObservableViewItem<T>(value.NewIndex) { Item = value.NewItem };
                        var subscription = _predicate(item.Item)
                            .ContinueWith<PredicateStateMachineFactory<T>, ObservableViewItem<T>>(new(item))
                            .Subscribe(ref this);

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

                        var newItem = new ObservableViewItem<T>(value.NewIndex) { Item = value.NewItem };
                        var newSubscription = _predicate(newItem.Item)
                            .ContinueWith<PredicateStateMachineFactory<T>, ObservableViewItem<T>>(new(newItem))
                            .Subscribe(ref this);

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
                                ListChange.Move<T>(
                                    oldIndexTranslated,
                                    newIndexTranslated));
                        }

                        break;
                    }
            }
        }

        public void OnNext(ObservableViewItem<T> value)
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

    private readonly struct WhereAsyncStateMachineFactory<T> : IStateMachineFactory<ListChange<T>, ListChange<T>>
    {
        private readonly Func<T, ObserverBuilder<bool>> _predicate;

        public WhereAsyncStateMachineFactory(Func<T, ObserverBuilder<bool>> predicate) =>
            _predicate = predicate;

        public void Create<TContinuation>(in TContinuation continuation, ObserverStateMachine<ListChange<T>> source)
            where TContinuation : struct, IStateMachine<ListChange<T>> =>
            source.ContinueWith(new WhereAsyncStateMachine<T, TContinuation>(continuation, _predicate));
    }

    private struct PredicateStateMachine<T, TContinuation> : IStateMachine<bool>
        where TContinuation : struct, IStateMachine<ObservableViewItem<T>>
    {
        private TContinuation _continuation;
        private readonly ObservableViewItem<T> _item;

        public PredicateStateMachine(in TContinuation continuation, ObservableViewItem<T> item)
        {
            _continuation = continuation;
            _item = item;
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

        public void OnNext(bool value)
        {
            if (_item.Present == value)
                return;

            _item.Present = value;

            if (_item.Initialized)
                _continuation.OnNext(_item);
        }
    }

    private readonly struct PredicateStateMachineFactory<T> : IStateMachineFactory<bool, ObservableViewItem<T>>
    {
        private readonly ObservableViewItem<T> _item;

        public PredicateStateMachineFactory(ObservableViewItem<T> item) =>
            _item = item;

        public void Create<TContinuation>(in TContinuation continuation, ObserverStateMachine<bool> source)
            where TContinuation : struct, IStateMachine<ObservableViewItem<T>> =>
            source.ContinueWith(new PredicateStateMachine<T, TContinuation>(continuation, _item));
    }
}