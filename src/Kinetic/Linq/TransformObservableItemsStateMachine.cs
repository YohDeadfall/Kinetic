using System;
using System.Collections.Generic;
using Kinetic.Runtime;

namespace Kinetic.Linq;

internal struct TransformObservableItemsStateMachine<TContinuation, TSource, TResult> :
    IStateMachine<ListChange<TSource>>,
    IObservableItemStateMachine<TResult>
    where TContinuation : struct, IStateMachine<ListChange<TResult>>
{
    private TContinuation _continuation;
    private ObservableItemStateMachineReference<TResult, ListChange<TSource>, TransformObservableItemsStateMachine<TContinuation, TSource, TResult>>? _reference;
    private readonly Func<TSource, IObservable<TResult>> _selector;
    private readonly List<ObservableViewItem<TResult>> _items = new();

    public TransformObservableItemsStateMachine(TContinuation continuation, Func<TSource, IObservable<TResult>> selector)
    {
        _continuation = continuation;
        _selector = selector;
    }

    public StateMachineBox Box =>
        _continuation.Box;

    private ObservableItemStateMachineReference<TResult, ListChange<TSource>, TransformObservableItemsStateMachine<TContinuation, TSource, TResult>> Reference =>
        _reference ??= new(ref this);

    StateMachineReference<ListChange<TSource>> IStateMachine<ListChange<TSource>>.Reference =>
        Reference;

    IObservableItemStateMachine<TResult> IObservableItemStateMachine<TResult>.Reference =>
        Reference;

    public StateMachineReference? Continuation =>
        _continuation.Reference;

    public void Dispose()
    {
        throw new NotImplementedException();
    }

    public void Initialize(StateMachineBox box) =>
        _continuation.Initialize(box);

    public void OnCompleted()
    {
        throw new NotImplementedException();
    }

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
                    ObservableViewItem<TResult> item;
                    try
                    {
                        item = new(value.NewIndex, _selector(value.NewItem), Reference);
                    }
                    catch (Exception error)
                    {
                        _continuation.OnError(error);
                        return;
                    }

                    _items.Insert(item.Index, item);

                    if (item.Present)
                    {
                        _continuation.OnNext(
                            ListChange.Insert(
                                index: CountBefore(item.Index),
                                item.Value));
                    }

                    break;
                }
            case ListChangeAction.Replace:
                {
                    var index = value.OldIndex;
                    var oldItem = _items[index];

                    oldItem.Dispose();

                    ObservableViewItem<TResult> newItem;
                    try
                    {
                        newItem = new(value.NewIndex, _selector(value.NewItem), Reference);
                    }
                    catch (Exception error)
                    {
                        _continuation.OnError(error);
                        return;
                    }

                    _items[index] = newItem;

                    if (oldItem.Present)
                    {
                        if (newItem.Present)
                        {
                            _continuation.OnNext(
                                ListChange.Replace(
                                    index: CountBefore(index),
                                    newItem.Value));
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
                                    newItem.Value));
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

    public void OnItemCompleted(ObservableViewItem<TResult> item, Exception? error)
    {
        if (error is { })
            _continuation.OnError(error);
    }

    public void OnItemUpdated(ObservableViewItem<TResult> item)
    {
        var index = CountBefore(item.Index);
        if (item.Present)
        {
            _continuation.OnNext(
                ListChange.Replace(index, item.Value));
        }
        else
        {
            item.Present = true;

            _continuation.OnNext(
                ListChange.Insert(index, item.Value));
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