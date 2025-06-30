using System;

namespace Kinetic.Linq;

internal interface IGroupItemsByStateMachine<TKey, TSource, TState>
    where TState : IGroupItemsByState<TKey, TSource>
{
    IGroupItemsByStateMachine<TKey, TSource, TState> Reference { get; }

    void AddItemDeferred(int index, TState item);
    void AddItem(int index, TState item, TSource source, TKey key);
    void UpdateItem(int index, TState item, TSource source, TKey key);
    void ReplaceItem(int index, TState item, TSource source, TKey key);

    void OnError(Exception error);
}