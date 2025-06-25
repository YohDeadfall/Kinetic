namespace Kinetic.Linq;

internal interface IGroupItemsByStateMachine<TState, TSource, TKey>
    where TState : IGroupItemsByState
{
    IGroupItemsByStateMachine<TState, TSource, TKey> Reference { get; }

    void AddItemDeferred(int index, TState item);
    void AddItem(int index, TState item, TSource source, TKey key);
    void UpdateItem(int index, TState item, TSource source, TKey key);
    void ReplaceItem(int index, TState item, TSource source, TKey key);
}