using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Kinetic.Linq.StateMachines;

namespace Kinetic.Linq;

public static partial class ObservableView
{
    public static ObserverBuilder<ListChange<TResult>> Select<TSource, TResult>(this ObserverBuilder<ListChange<TSource>> source, Func<TSource, TResult> selector) =>
        source.ContinueWith<SelectStateMachineFactory<TSource, TResult>, ListChange<TResult>>(new(selector));

    public static ObserverBuilder<ListChange<TResult>> Select<TSource, TResult>(this ReadOnlyObservableList<TSource> source, Func<TSource, TResult> selector) =>
        source.Changed.ToBuilder().Select(selector);

    private struct SelectStateMachine<TSource, TResult, TContinuation> : IObserverStateMachine<ListChange<TSource>>
        where TContinuation : struct, IObserverStateMachine<ListChange<TResult>>
    {
        private TContinuation _continuation;
        private Func<TSource, TResult> _selector;
        private List<TResult> _items = new();
        private ObserverStateMachineBox? _box;

        public SelectStateMachine(in TContinuation continuation, Func<TSource, TResult> selector)
        {
            _continuation = continuation;
            _selector = selector;
        }

        public void Dispose() =>
            _continuation.Dispose();

        public void Initialize(ObserverStateMachineBox box)
        {
            _box = box;
            _continuation.Initialize(box);
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
                        _items.Clear();
                        _continuation.OnNext(
                            ListChange.RemoveAll<TResult>());
                        break;
                    }
                case ListChangeAction.Remove:
                    {
                        var index = value.OldIndex;

                        _items.RemoveAt(index);
                        _continuation.OnNext(
                            ListChange.Remove<TResult>(index));

                        break;
                    }
                case ListChangeAction.Insert:
                    {
                        var index = value.NewIndex;
                        var item = _selector(value.NewItem);

                        _items.Insert(index, item);
                        _continuation.OnNext(
                            ListChange.Insert(index, item));

                        break;
                    }
                case ListChangeAction.Replace:
                    {
                        var index = value.OldIndex;
                        var item = _selector(value.NewItem);

                        _items[index] = item;
                        _continuation.OnNext(
                            ListChange.Replace(index, item));

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

                        _continuation.OnNext(
                            ListChange.Move(oldIndex, newIndex, item));

                        break;
                    }
            }
        }
    }

    private readonly struct SelectStateMachineFactory<TSource, TResult> : IObserverStateMachineFactory<ListChange<TSource>, ListChange<TResult>>
    {
        private readonly Func<TSource, TResult> _selector;

        public SelectStateMachineFactory(Func<TSource, TResult> selector) =>
            _selector = selector;

        public void Create<TContinuation>(in TContinuation continuation, ObserverStateMachine<ListChange<TSource>> source)
            where TContinuation : struct, IObserverStateMachine<ListChange<TResult>> =>
            source.ContinueWith(new SelectStateMachine<TSource, TResult, TContinuation>(continuation, _selector));
    }
}