using System;
using System.Collections.Generic;
using Kinetic.Runtime;

namespace Kinetic.Linq;

internal struct TransformItemsStateMachine<TContinuation, TTransform, TSource, TResult> : IStateMachine<ListChange<TSource>>
    where TContinuation : struct, IStateMachine<ListChange<TResult>>
    where TTransform : struct, ITransform<TSource, TResult>
{
    private TContinuation _continuation;
    private TTransform _transform;
    private readonly List<TResult> _items = new();

    public TransformItemsStateMachine(TContinuation continuation, TTransform transform)
    {
        _continuation = continuation;
        _transform = transform;
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
                    TResult item;

                    try
                    {
                        item = _transform.Transform(value.NewItem);
                    }
                    catch (Exception error)
                    {
                        _continuation.OnError(error);
                        return;
                    }

                    _items.Insert(index, item);
                    _continuation.OnNext(
                        ListChange.Insert(index, item));

                    break;
                }
            case ListChangeAction.Replace:
                {
                    var index = value.OldIndex;
                    TResult item;

                    try
                    {
                        item = _transform.Transform(value.NewItem);
                    }
                    catch (Exception error)
                    {
                        _continuation.OnError(error);
                        return;
                    }

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
                        ListChange.Move<TResult>(oldIndex, newIndex));

                    break;
                }
        }
    }
}