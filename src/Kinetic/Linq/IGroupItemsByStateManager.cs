using System;

namespace Kinetic.Linq;

internal interface IGroupItemsByStateManager<TKey, TSource, TState>
    where TState : IGroupItemsByState<TKey, TSource>
{
    void Create<TGroupBy>(int sourceIndex, TSource source, ref TGroupBy groupBy, bool replacement)
        where TGroupBy : struct, IGroupItemsByStateMachine<TKey, TSource, TState>;

    void Dispose(TState item);
    void DisposeAll(ReadOnlySpan<TState> items);

    void SetOriginalIndex(TState item, int index);
    void SetOriginalIndexes(ReadOnlySpan<TState> items, int indexChange);
}