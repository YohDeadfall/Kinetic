using System;

namespace Kinetic.Linq;

internal interface IOrderItemsByStateMachine<TState, TSource, TKey>
    where TState : IOrderItemsByState<TKey>
{
    IOrderItemsByStateMachine<TState, TSource, TKey> Reference { get; }

    void UpdateItem(int index, TState item);
    void OnError(Exception error);
}