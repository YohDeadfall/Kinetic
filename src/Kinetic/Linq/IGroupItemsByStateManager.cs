using System;

namespace Kinetic.Linq;

internal interface IGroupItemsByStateManager<TState, TSource, TKey>
    where TState : IGroupItemsByState
{
    void Create<TGroupBy>(int sourceIndex, TSource source, ref TGroupBy groupBy, bool replacement)
        where TGroupBy : struct, IGroupItemsByStateMachine<TState, TSource, TKey>;

    void Dispose(TState item);
    void DisposeAll(ReadOnlySpan<TState> items);

    void SetOriginalIndex(TState item, int index);
    void SetOriginalIndexes(ReadOnlySpan<TState> items, int indexChange);
}