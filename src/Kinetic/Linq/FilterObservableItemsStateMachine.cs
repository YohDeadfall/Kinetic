using System;
using System.Collections;
using System.Collections.Generic;
using Kinetic.Runtime;

namespace Kinetic.Linq;

internal struct FilterObservableItemsStateMachine<TContinuation, TSource> : IStateMachine<ListChange<TSource>>, IReadOnlyList<TSource>
    where TContinuation : struct, IStateMachine<ListChange<TSource>>
{
    private TContinuation _continuation;
    private ListStateMachineReference<TSource, FilterObservableItemsStateMachine<TContinuation, TSource>>? _reference;
    private readonly Func<TSource, IObservable<bool>> _predicate;
    private readonly List<Item> _items = new();

    public FilterObservableItemsStateMachine(TContinuation continuation, Func<TSource, IObservable<bool>> predicate)
    {
        _continuation = continuation;
        _predicate = predicate;
    }

    public StateMachineBox Box =>
        _continuation.Box;

    private ListStateMachineReference<TSource, FilterObservableItemsStateMachine<TContinuation, TSource>> Reference =>
        _reference ??= new(ref this);

    StateMachineReference<ListChange<TSource>> IStateMachine<ListChange<TSource>>.Reference =>
        Reference;

    public StateMachineReference? Continuation =>
        _continuation.Reference;

    int IReadOnlyCollection<TSource>.Count =>
        _items.Count;

    TSource IReadOnlyList<TSource>.this[int index] =>
        _items[index].Value;

    public void Dispose()
    {
        foreach (var item in _items)
            item.Dispose();

        _continuation.Dispose();
    }

    public void Initialize(StateMachineBox box) =>
        _continuation.Initialize(box);

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
                    _continuation.OnNext(value);

                    break;
                }
            case ListChangeAction.Remove:
                {
                    var item = Item.Remove(_items, value.OldIndex);
                    if (item.Present)
                    {
                        _continuation.OnNext(
                            ListChange.Remove<TSource>(
                                Item.GetAdjustedIndex(_items, item.Index)));
                    }

                    break;
                }
            case ListChangeAction.Insert:
            case ListChangeAction.Replace:
                {
                    Item.TryCreate(
                        value.NewIndex,
                        value.NewItem,
                        ref this,
                        value.Action == ListChangeAction.Replace);
                    break;
                }
            case ListChangeAction.Move when
                value.OldIndex is var oldIndex &&
                value.NewIndex is var newIndex &&
                newIndex != oldIndex:
                {
                    var item = Item.Move(_items, oldIndex, newIndex);
                    if (item.Present)
                    {
                        var oldAdjustedIndex = Item.GetAdjustedIndex(_items, oldIndex);
                        var newAdjustedIndex = Item.GetAdjustedIndex(_items, newIndex);

                        if (newAdjustedIndex > oldAdjustedIndex)
                            newAdjustedIndex -= 1;

                        _continuation.OnNext(
                            ListChange.Move<TSource>(
                                oldAdjustedIndex,
                                newAdjustedIndex));
                    }

                    break;
                }
        }
    }

    public IEnumerator<TSource> GetEnumerator()
    {
        foreach (var item in _items)
            yield return item.Value;
    }

    IEnumerator IEnumerable.GetEnumerator() =>
        GetEnumerator();

    private sealed class Item : ObservableViewItem, IObserver<bool>
    {
        private readonly ListStateMachineReference<TSource, FilterObservableItemsStateMachine<TContinuation, TSource>> _stateMachine;
        private readonly IDisposable _subscription;

        public TSource Value { get; }

        private Item(int index, TSource item, ref FilterObservableItemsStateMachine<TContinuation, TSource> stateMachine, bool replace) :
            base(index)
        {
            Present = replace;
            Value = item;

            _stateMachine = stateMachine.Reference;
            _subscription = stateMachine._predicate(item).Subscribe(this);
        }

        public static Item? TryCreate(int index, TSource item, ref FilterObservableItemsStateMachine<TContinuation, TSource> stateMachine, bool replace)
        {
            try
            {
                return new(index, item, ref stateMachine, replace);
            }
            catch (Exception error)
            {
                stateMachine.OnError(error);
                return null;
            }
        }

        public override void Dispose() =>
            _subscription.Dispose();

        public void OnCompleted() { }

        public void OnError(Exception error) =>
            _stateMachine.Target.Dispose();

        public void OnNext(bool value)
        {
            if (_subscription is { })
            {
                if (Present == value)
                    return;

                Present = value;

                ref var stateMachine = ref _stateMachine.Target;
                var adjustedIndex = Item.GetAdjustedIndex(stateMachine._items, Index);

                stateMachine._continuation.OnNext(
                    Present
                        ? ListChange.Insert(adjustedIndex, Value)
                        : ListChange.Remove<TSource>(adjustedIndex));
            }
            else
            {
                // The result was immediately provided by the observer
                // as the constructor hasn't finished yet.
                ref var stateMachine = ref _stateMachine.Target;
                var adjustedIndex = Item.GetAdjustedIndex(stateMachine._items, Index);

                // Presence at this point just tells that it's a replacement.
                // Therefore, the value must be updated before anything else.
                Present = value;

                if (value)
                {
                    // Replacement initiated by the parent state machine.
                    var oldItem = Item.Replace(stateMachine._items, Index, this);
                    stateMachine._continuation.OnNext(
                        oldItem.Present
                            ? ListChange.Replace(adjustedIndex, Value)
                            : ListChange.Remove<TSource>(adjustedIndex));
                }
                else
                {
                    // First time insertion.
                    Item.Insert(stateMachine._items, Index, this);

                    stateMachine._continuation.OnNext(
                        ListChange.Insert(adjustedIndex, Value));
                }
            }
        }
    }
}