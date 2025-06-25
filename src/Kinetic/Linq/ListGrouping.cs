using System;

namespace Kinetic.Linq;

internal sealed class ListGrouping<TKey, TSource> : IObservableInternal<ListChange<TSource>>, IGrouping<TKey, ListChange<TSource>>
{
    private ObservableSubscriptions<ListChange<TSource>> _subscriptions;
    private int _count;

    public bool IsEmpty => _count == 0;

    public required TKey Key { get; init; }

    public IDisposable Subscribe(IObserver<ListChange<TSource>> observer) =>
        _subscriptions.Subscribe(observer, this);

    public void Subscribe(ObservableSubscription<ListChange<TSource>> subscription) =>
        _subscriptions.Subscribe(subscription, this);

    public void Unsubscribe(ObservableSubscription<ListChange<TSource>> subscription) =>
        _subscriptions.Unsubscribe(subscription);

    public int Add(TSource value)
    {
        var index = _count;

        _subscriptions.OnNext(ListChange.Insert(index, value));
        _count += 1;

        return index;
    }

    public void Remove(int index)
    {
        _subscriptions.OnNext(ListChange.Remove<TSource>(index));
        _count -= 1;
    }

    public void Replace(int index, TSource value) =>
        _subscriptions.OnNext(ListChange.Replace(index, value));
}