using System;

namespace Kinetic.Linq;

internal interface IOrderItemsByStateManager<TState, TSource, TKey>
    where TState : IOrderItemsByState<TKey>
{
    TState CreateItem<TOrderBy>(int sourceIndex, TSource source, ref TOrderBy orderBy)
        where TOrderBy : struct, IOrderItemsByStateMachine<TState, TSource, TKey>;

    void DisposeItem(TState item);
    void DisposeItems(ReadOnlySpan<TState> items);
}