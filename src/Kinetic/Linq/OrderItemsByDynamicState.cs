using System;

namespace Kinetic.Linq;

internal sealed class OrderItemsByDynamicState<TKey, TSource> : IOrderItemsByState<TKey>, IObserver<TKey>
{
    private readonly TSource _source;
    private readonly IDisposable _subscription;
    private readonly IOrderItemsByStateMachine<OrderItemsByDynamicState<TKey, TSource>, TSource, TKey> _orderBy;

    private OrderItemsByDynamicState(
        int sourceIndex,
        TSource source,
        Func<TSource, IObservable<TKey>> keySelector,
        IOrderItemsByStateMachine<OrderItemsByDynamicState<TKey, TSource>, TSource, TKey> orderBy)
    {
        _source = source;
        _orderBy = orderBy;
        _subscription = keySelector(source).Subscribe(this);

        Key = default!;
    }

    public int Index { get; set; }

    public TKey Key { get; private set; }

    public void OnCompleted() { }

    public void OnError(Exception error) =>
        _orderBy.OnError(error);

    public void OnNext(TKey value)
    {
        Key = value;

        if (_subscription is { })
            _orderBy.UpdateItem(default, this);
    }

    public readonly struct Manager(Func<TSource, IObservable<TKey>> KeySelector) :
        IOrderItemsByStateManager<OrderItemsByDynamicState<TKey, TSource>, TSource, TKey>
    {
        public OrderItemsByDynamicState<TKey, TSource> CreateItem<TOrderBy>(int sourceIndex, TSource source, ref TOrderBy orderBy)
            where TOrderBy : struct, IOrderItemsByStateMachine<OrderItemsByDynamicState<TKey, TSource>, TSource, TKey>
        {
            return new(sourceIndex, source, KeySelector, orderBy.Reference);
        }

        public void DisposeItem(OrderItemsByDynamicState<TKey, TSource> item)
        {
            item._subscription.Dispose();
        }

        public void DisposeItems(ReadOnlySpan<OrderItemsByDynamicState<TKey, TSource>> items)
        {
            foreach (var item in items)
                DisposeItem(item);
        }
    }
}