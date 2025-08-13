using System;
using Kinetic.Runtime;

namespace Kinetic.Linq;

internal struct FilterItemsStateMachine<TContinuation, TFilter, TSource> : IStateMachine<ListChange<TSource>>
    where TContinuation : struct, IStateMachine<ListChange<TSource>>
    where TFilter : struct, ITransform<TSource, bool>
{
    private TContinuation _continuation;
    private TFilter _filter;
    private ValueBitmap _bitmap;

    public FilterItemsStateMachine(TContinuation continuation, TFilter filter)
    {
        _continuation = continuation;
        _filter = filter;
    }

    public StateMachineBox Box =>
        _continuation.Box;

    public StateMachineReference<ListChange<TSource>> Reference =>
        StateMachineReference<ListChange<TSource>>.Create(ref this);

    public StateMachineReference? Continuation =>
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
                    _bitmap.RemoveAll();
                    _continuation.OnNext(value);

                    break;
                }
            case ListChangeAction.Remove:
                {
                    var index = value.OldIndex;
                    var presence = _bitmap[index];

                    _bitmap.RemoveAt(index);

                    if (presence)
                    {
                        _continuation.OnNext(
                            ListChange.Remove<TSource>(
                                index: _bitmap.PopCountBefore(index)));
                    }

                    break;
                }
            case ListChangeAction.Insert:
                {
                    var index = value.NewIndex;
                    var item = value.NewItem;
                    bool presence;

                    try
                    {
                        presence = _filter.Transform(item);
                    }
                    catch (Exception error)
                    {
                        _continuation.OnError(error);
                        return;
                    }

                    _bitmap.Insert(index, presence);

                    if (presence)
                    {
                        _continuation.OnNext(
                            ListChange.Insert(
                                index: _bitmap.PopCountBefore(index),
                                item));
                    }

                    break;
                }
            case ListChangeAction.Replace:
                {
                    var index = value.OldIndex;
                    var item = value.NewItem;

                    var oldPresence = _bitmap[index];
                    bool newPresence;

                    try
                    {
                        newPresence = _filter.Transform(item);
                    }
                    catch (Exception error)
                    {
                        _continuation.OnError(error);
                        return;
                    }

                    _bitmap[index] = newPresence;

                    if (oldPresence)
                    {
                        if (newPresence)
                        {
                            _continuation.OnNext(
                                ListChange.Replace(
                                    index: _bitmap.PopCountBefore(index),
                                    item));
                        }
                        else
                        {
                            _continuation.OnNext(
                                ListChange.Remove<TSource>(
                                    index: _bitmap.PopCountBefore(index)));
                        }
                    }
                    else
                    {
                        if (newPresence)
                        {
                            _continuation.OnNext(
                                ListChange.Insert(
                                    index: _bitmap.PopCountBefore(index),
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
                    _bitmap.Move(oldIndex, newIndex);

                    if (_bitmap[newIndex])
                    {
                        var oldIndexTranslated = _bitmap.PopCountBefore(oldIndex);
                        var newIndexTranslated = _bitmap.PopCountBefore(newIndex);

                        if (newIndexTranslated > oldIndexTranslated)
                        {
                            newIndexTranslated -= 1;
                        }

                        _continuation.OnNext(
                            ListChange.Move<TSource>(
                                oldIndexTranslated,
                                newIndexTranslated));
                    }

                    break;
                }
        }
    }
}