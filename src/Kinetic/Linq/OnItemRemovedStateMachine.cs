using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Kinetic.Runtime;

namespace Kinetic.Linq;

internal struct OnItemRemovedStateMachine<TContinuation, TSource> : IStateMachine<ListChange<TSource>>, IReadOnlyList<TSource>
    where TContinuation : struct, IStateMachine<ListChange<TSource>>
{
    private TContinuation _continuation;
    private IReadOnlyList<TSource>? _items;
    private readonly Action<TSource> _onRemoved;

    public OnItemRemovedStateMachine(in TContinuation continuation, Action<TSource> onRemoved)
    {
        _continuation = continuation;
        _onRemoved = onRemoved;
    }

    public StateMachineBox Box =>
        _continuation.Box;

    public int Count =>
        _items!.Count;

    public StateMachineReference<ListChange<TSource>> Reference =>
        new ListStateMachineReference<TSource, OnItemRemovedStateMachine<TContinuation, TSource>>(ref this);

    public StateMachineReference? Continuation =>
        _continuation.Reference;

    public TSource this[int index] =>
        _items![index];

    public void Initialize(StateMachineBox box)
    {
        _continuation.Initialize(box);
        _items = _continuation.Reference as IReadOnlyList<TSource> ?? new List<TSource>();
    }

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
                    var buffer = _items!.ToArray();

                    _continuation.OnNext(value);

                    try
                    {
                        foreach (var item in buffer)
                            _onRemoved(item);
                    }
                    catch (Exception error)
                    {
                        _continuation.OnError(error);
                        return;
                    }

                    if (_items is List<TSource> items)
                        items.Clear();

                    break;
                }
            case ListChangeAction.Remove:
                {
                    var index = value.OldIndex;
                    var item = _items![value.OldIndex];

                    _continuation.OnNext(value);

                    try
                    {
                        _onRemoved(item);
                    }
                    catch (Exception error)
                    {
                        _continuation.OnError(error);
                        return;
                    }

                    if (_items is List<TSource> items)
                        items.RemoveAt(index);

                    break;
                }
            case ListChangeAction.Insert:
                {
                    _continuation.OnNext(value);

                    if (_items is List<TSource> items)
                        items.Insert(value.NewIndex, value.NewItem);

                    break;
                }
            case ListChangeAction.Replace:
                {
                    var index = value.OldIndex;
                    var item = _items![index];

                    _continuation.OnNext(value);

                    try
                    {
                        _onRemoved(item);
                    }
                    catch (Exception error)
                    {
                        _continuation.OnError(error);
                        return;
                    }

                    if (_items is List<TSource> items)
                        items[index] = value.NewItem;

                    break;
                }
            case ListChangeAction.Move:
                {
                    _continuation.OnNext(value);

                    if (_items is List<TSource> items)
                    {
                        var index = value.OldIndex;
                        var item = items[index];

                        items.RemoveAt(index);
                        items.Insert(index, item);
                    }

                    break;
                }
        }
    }

    public IEnumerator<TSource> GetEnumerator() =>
        _items!.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() =>
        _items!.GetEnumerator();
}