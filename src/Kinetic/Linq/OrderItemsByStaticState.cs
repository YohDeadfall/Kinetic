using System;
using System.Collections.Generic;

namespace Kinetic.Linq;

internal struct OrderItemsByStaticState<TKey> : IOrderItemsByState<TKey>, IComparable<OrderItemsByStaticState<TKey>>
{
    public int Index { get; set; }
    public TKey Key { get; }

    private OrderItemsByStaticState(int index, TKey key) =>
        Key = key;

    public int CompareTo(OrderItemsByStaticState<TKey> other) =>
        Comparer<TKey>.Default.Compare(Key, other.Key);

    public readonly struct Manager<TSource>(Func<TSource, TKey> KeySelector) :
        IOrderItemsByStateManager<OrderItemsByStaticState<TKey>, TSource, TKey>
    {
        public OrderItemsByStaticState<TKey> CreateItem<TOrderBy>(int sourceIndex, TSource source, ref TOrderBy orderBy)
            where TOrderBy : struct, IOrderItemsByStateMachine<OrderItemsByStaticState<TKey>, TSource, TKey>
        {
            return new(sourceIndex, KeySelector(source));
        }

        public void DisposeItem(OrderItemsByStaticState<TKey> item) { }

        public void DisposeItems(ReadOnlySpan<OrderItemsByStaticState<TKey>> items) { }
    }
}