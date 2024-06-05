using System;
using System.Collections.Generic;
using Kinetic.Linq.StateMachines;

namespace Kinetic.Linq;

public static partial class ObservableView
{
    public static ObserverBuilder<ListChange<TResult>> Select<TSource, TResult>(this ObserverBuilder<ListChange<TSource>> source, Func<TSource, ObserverBuilder<TResult>> selector) =>
        source.ContinueWith<SelectAsyncStateMachineFactory<TSource, TResult>, ListChange<TResult>>(new(selector));

    public static ObserverBuilder<ListChange<TResult>> Select<TSource, TResult>(this ObserverBuilder<ListChange<TSource>> source, Func<TSource, Property<TResult>> selector) =>
        source.Select((item) => selector(item).Changed.ToBuilder());

    public static ObserverBuilder<ListChange<TResult>> Select<TSource, TResult>(this ObserverBuilder<ListChange<TSource>> source, Func<TSource, ReadOnlyProperty<TResult>> selector) =>
        source.Select((item) => selector(item).Changed.ToBuilder());

    public static ObserverBuilder<ListChange<TResult>> Select<TSource, TResult>(this ObserverBuilder<ListChange<TSource>> source, Func<TSource, IObservable<TResult>> selector) =>
        source.Select((item) => selector(item).ToBuilder());

    public static ObserverBuilder<ListChange<TResult>> Select<TSource, TResult>(this ReadOnlyObservableList<TSource> source, Func<TSource, ObserverBuilder<TResult>> selector) =>
        source.Changed.ToBuilder().Select(selector);

    public static ObserverBuilder<ListChange<TResult>> Select<TSource, TResult>(this ReadOnlyObservableList<TSource> source, Func<TSource, Property<TResult>> selector) =>
        source.Changed.ToBuilder().Select((item) => selector(item).Changed.ToBuilder());

    public static ObserverBuilder<ListChange<TResult>> Select<TSource, TResult>(this ReadOnlyObservableList<TSource> source, Func<TSource, ReadOnlyProperty<TResult>> selector) =>
        source.Changed.ToBuilder().Select((item) => selector(item).Changed.ToBuilder());

    public static ObserverBuilder<ListChange<TResult>> Select<TSource, TResult>(this ReadOnlyObservableList<TSource> source, Func<TSource, IObservable<TResult>> selector) =>
        source.Changed.ToBuilder().Select((item) => selector(item).ToBuilder());

    private struct SelectAsyncStateMachine<TSource, TResult, TContinuation> :
        IStateMachine<ListChange<TSource>>,
        IStateMachine<ObservableViewItem<TResult>>
        where TContinuation : struct, IStateMachine<ListChange<TResult>>
    {
        private readonly Func<TSource, ObserverBuilder<TResult>> _selector;
        private readonly List<ObservableViewItem<TResult>> _items = new();
        private TContinuation _continuation;

        public SelectAsyncStateMachine(in TContinuation continuation, Func<TSource, ObserverBuilder<TResult>> selector)
        {
            _continuation = continuation;
            _selector = selector;
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

        public void OnNext(ListChange<TSource> value)
        {
            switch (value.Action)
            {
                case ListChangeAction.RemoveAll:
                    {
                        foreach (var item in _items)
                            item.Dispose();

                        _items.Clear();
                        _continuation.OnNext(
                            ListChange.RemoveAll<TResult>());

                        break;
                    }
                case ListChangeAction.Remove:
                    {
                        var index = value.OldIndex;
                        var item = _items[index];

                        _items[index].Dispose();
                        _items.RemoveAt(index);

                        if (item.Present)
                        {
                            _continuation.OnNext(
                                ListChange.Remove<TResult>(CountBefore(index)));
                        }

                        break;
                    }
                case ListChangeAction.Insert:
                    {
                        var index = value.NewIndex;
                        var item = new ObservableViewItem<TResult>(index);
                        var subscription = _selector(value.NewItem)
                            .ContinueWith<SelectorStateMachineFactory<TResult>, ObservableViewItem<TResult>>(new(item))
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

                        var newItem = new ObservableViewItem<TResult>(value.NewIndex);
                        var newSubscription = _selector(value.NewItem)
                            .ContinueWith<SelectorStateMachineFactory<TResult>, ObservableViewItem<TResult>>(new(newItem))
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
                                    ListChange.Remove<TResult>(
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
                                ListChange.Move<TResult>(
                                    oldIndexTranslated,
                                    newIndexTranslated));
                        }

                        break;
                    }
            }
        }

        public void OnNext(ObservableViewItem<TResult> value)
        {
            var index = CountBefore(value.Index);

            if (value.Present)
            {
                _continuation.OnNext(
                    ListChange.Replace(index, value.Item));
            }
            else
            {
                value.Present = true;

                _continuation.OnNext(
                    ListChange.Insert(index, value.Item));
            }
        }

        private int CountBefore(int index)
        {
            var count = 0;

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

    private readonly struct SelectAsyncStateMachineFactory<TSource, TResult> : IStateMachineFactory<ListChange<TSource>, ListChange<TResult>>
    {
        private readonly Func<TSource, ObserverBuilder<TResult>> _selector;

        public SelectAsyncStateMachineFactory(Func<TSource, ObserverBuilder<TResult>> selector) =>
            _selector = selector;

        public void Create<TContinuation>(in TContinuation continuation, ObserverStateMachine<ListChange<TSource>> source)
            where TContinuation : struct, IStateMachine<ListChange<TResult>> =>
            source.ContinueWith(new SelectAsyncStateMachine<TSource, TResult, TContinuation>(continuation, _selector));
    }

    private struct SelectorStateMachine<T, TContinuation> : IStateMachine<T>
        where TContinuation : struct, IStateMachine<ObservableViewItem<T>>
    {
        private TContinuation _continuation;
        private readonly ObservableViewItem<T> _item;

        public SelectorStateMachine(in TContinuation continuation, ObservableViewItem<T> item)
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

        public void OnNext(T value)
        {
            _item.Item = value;

            if (_item.Initialized)
                _continuation.OnNext(_item);
            else
                _item.Present = true;
        }
    }

    private readonly struct SelectorStateMachineFactory<T> : IStateMachineFactory<T, ObservableViewItem<T>>
    {
        private readonly ObservableViewItem<T> _item;

        public SelectorStateMachineFactory(ObservableViewItem<T> item) =>
            _item = item;

        public void Create<TContinuation>(in TContinuation continuation, ObserverStateMachine<T> source)
            where TContinuation : struct, IStateMachine<ObservableViewItem<T>> =>
            source.ContinueWith(new SelectorStateMachine<T, TContinuation>(continuation, _item));
    }
}