using System;
using Kinetic.Linq.StateMachines;

namespace Kinetic.Linq;

public static partial class ObservableView
{
    public static ObserverBuilder<ListChange<T>> Where<T>(this ObserverBuilder<ListChange<T>> source, Func<T, bool> predicate) =>
        source.ContinueWith<WhereStateMachineFactory<T>, ListChange<T>>(new(predicate));

    public static ObserverBuilder<ListChange<T>> Where<T>(this ReadOnlyObservableList<T> source, Func<T, bool> predicate) =>
        source.Changed.ToBuilder().Where(predicate);

    private struct WhereStateMachine<T, TContinuation> : IObserverStateMachine<ListChange<T>>
        where TContinuation : struct, IObserverStateMachine<ListChange<T>>
    {
        private TContinuation _continuation;
        private ValueBitmap _itemPresence;
        private readonly Func<T, bool> _predicate;

        public WhereStateMachine(in TContinuation continuation, Func<T, bool> predicate)
        {
            _continuation = continuation;
            _predicate = predicate;
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
                        _itemPresence.RemoveAll();
                        _continuation.OnNext(value);

                        break;
                    }
                case ListChangeAction.Remove:
                    {
                        var index = value.OldIndex;
                        var itemPresence = _itemPresence[index];

                        _itemPresence.RemoveAt(index);

                        if (itemPresence)
                        {
                            _continuation.OnNext(
                                ListChange.Remove<T>(
                                    index: _itemPresence.PopCountBefore(index)));
                        }

                        break;
                    }
                case ListChangeAction.Insert:
                    {
                        var index = value.NewIndex;
                        var item = value.NewItem;
                        var itemPresence = _predicate(item);

                        _itemPresence.Insert(index, itemPresence);

                        if (itemPresence)
                        {
                            _continuation.OnNext(
                                ListChange.Insert(
                                    index: _itemPresence.PopCountBefore(index),
                                    item));
                        }

                        break;
                    }
                case ListChangeAction.Replace:
                    {
                        var index = value.OldIndex;
                        var item = value.NewItem;

                        var newPresence = _predicate(item);
                        var oldPresence = _itemPresence[index];

                        _itemPresence[index] = newPresence;

                        if (oldPresence)
                        {
                            if (newPresence)
                            {
                                _continuation.OnNext(
                                    ListChange.Replace(
                                        index: _itemPresence.PopCountBefore(index),
                                        item));
                            }
                            else
                            {
                                _continuation.OnNext(
                                    ListChange.Remove<T>(
                                        index: _itemPresence.PopCountBefore(index)));
                            }
                        }
                        else
                        {
                            if (newPresence)
                            {
                                _continuation.OnNext(
                                    ListChange.Insert(
                                        index: _itemPresence.PopCountBefore(index),
                                        item));
                            }
                        }

                        break;
                    }
                case ListChangeAction.Move when
                    value.OldIndex is var oldIndex &&
                    value.NewIndex is var newIndex &&
                    newIndex != oldIndex:
                    {
                        _itemPresence.Move(oldIndex, newIndex);

                        if (_itemPresence[newIndex])
                        {
                            var oldIndexTranslated = _itemPresence.PopCountBefore(oldIndex);
                            var newIndexTranslated = _itemPresence.PopCountBefore(newIndex);

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
    }

    private readonly struct WhereStateMachineFactory<T> : IObserverStateMachineFactory<ListChange<T>, ListChange<T>>
    {
        private readonly Func<T, bool> _predicate;

        public WhereStateMachineFactory(Func<T, bool> predicate) =>
            _predicate = predicate;

        public void Create<TContinuation>(in TContinuation continuation, ObserverStateMachine<ListChange<T>> source)
            where TContinuation : struct, IObserverStateMachine<ListChange<T>> =>
            source.ContinueWith(new WhereStateMachine<T, TContinuation>(continuation, _predicate));
    }
}