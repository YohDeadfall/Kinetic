using System;
using System.Collections.Generic;
using Kinetic.Runtime;

namespace Kinetic.Linq;

internal struct TransformObservableItemsStateMachine<TContinuation, TSource, TResult> : IStateMachine<ListChange<TSource>>
    where TContinuation : struct, IStateMachine<ListChange<TResult>>
{
    private TContinuation _continuation;
    private StateMachineReference<ListChange<TSource>, TransformObservableItemsStateMachine<TContinuation, TSource, TResult>>? _reference;
    private readonly Func<TSource, IObservable<TResult>> _selector;
    private readonly List<Item> _items = new();

    public TransformObservableItemsStateMachine(TContinuation continuation, Func<TSource, IObservable<TResult>> selector)
    {
        _continuation = continuation;
        _selector = selector;
    }

    public StateMachineBox Box =>
        _continuation.Box;

    private StateMachineReference<ListChange<TSource>, TransformObservableItemsStateMachine<TContinuation, TSource, TResult>> Reference =>
        _reference ??= new(ref this);

    StateMachineReference<ListChange<TSource>> IStateMachine<ListChange<TSource>>.Reference =>
        Reference;

    public StateMachineReference? Continuation =>
        _continuation.Reference;

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
                    _continuation.OnNext(
                        ListChange.RemoveAll<TResult>());

                    break;
                }
            case ListChangeAction.Remove:
                {
                    var item = Item.Remove(_items, value.OldIndex);
                    if (item.Present)
                    {
                        _continuation.OnNext(
                            ListChange.Remove<TResult>(
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
                            ListChange.Move<TResult>(
                                oldAdjustedIndex,
                                newAdjustedIndex));
                    }

                    break;
                }
        }
    }

    private sealed class Item : ObservableViewItem, IObserver<TResult>
    {
        private readonly StateMachineReference<ListChange<TSource>, TransformObservableItemsStateMachine<TContinuation, TSource, TResult>> _stateMachine;
        private readonly IDisposable _subscription;

        private Item(int index, TSource item, ref TransformObservableItemsStateMachine<TContinuation, TSource, TResult> stateMachine, bool replace) :
            base(index)
        {
            Present = replace;

            _stateMachine = stateMachine.Reference;
            _subscription = stateMachine._selector(item).Subscribe(this);
        }

        public static Item? TryCreate(int index, TSource item, ref TransformObservableItemsStateMachine<TContinuation, TSource, TResult> stateMachine, bool replace)
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
            _stateMachine.OnError(error);

        public void OnNext(TResult value)
        {
            ref var stateMachine = ref _stateMachine.Target;
            if (_subscription is { })
            {
                // Change notification handling.
                var adjustedIndex = Item.GetAdjustedIndex(stateMachine._items, Index);
                if (Present)
                {
                    stateMachine._continuation.OnNext(
                        ListChange.Replace(adjustedIndex, value));
                }
                else
                {
                    Present = true;

                    stateMachine._continuation.OnNext(
                        ListChange.Insert(adjustedIndex, value));
                }
            }
            else
            {
                // The result was immediately provided by the observer
                // as the constructor hasn't finished yet.
                var index = Index;
                var adjustedIndex = Item.GetAdjustedIndex(stateMachine._items, index);

                if (Present)
                {
                    // Replacement initiated by the parent state machine.
                    var oldItem = Item.Replace(stateMachine._items, index, this);
                    if (oldItem.Present)
                    {
                        stateMachine._continuation.OnNext(
                            ListChange.Replace(adjustedIndex, item: value));
                        return;
                    }
                }

                Present = true;
                Item.Insert(stateMachine._items, index, this);

                stateMachine._continuation.OnNext(
                    ListChange.Insert(adjustedIndex, item: value));
            }
        }
    }
}