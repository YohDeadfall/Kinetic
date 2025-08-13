using System;

namespace Kinetic.Linq;

internal sealed class GroupItemsByDynamicState<TKey, TSource> : IGroupItemsByState<TKey, TSource>, IObserver<TKey>
{
    private int _sourceIndex;
    private readonly TSource _source;
    private readonly IDisposable _subscription;
    private readonly IGroupItemsByStateMachine<TKey, TSource, GroupItemsByDynamicState<TKey, TSource>> _groupBy;

    public ListGrouping<TKey, TSource>? Grouping { get; set; }
    public int Index { get; set; }

    private GroupItemsByDynamicState(
        bool replacement,
        int sourceIndex,
        TSource source,
        Func<TSource, IObservable<TKey>> keySelector,
        IGroupItemsByStateMachine<TKey, TSource, GroupItemsByDynamicState<TKey, TSource>> groupBy)
    {
        Grouping = null;
        Index = replacement ? 0 : -1;

        _source = source;
        _sourceIndex = sourceIndex;
        _groupBy = groupBy;
        _subscription = keySelector(source).Subscribe(this);
    }

    public void OnCompleted() { }

    public void OnError(Exception error) =>
        _groupBy.OnError(error);

    public void OnNext(TKey value)
    {
        if (_subscription is { })
        {
            if (Index != -1)
                _groupBy.UpdateItem(_sourceIndex, this, _source, value);
            else
                _groupBy.ReplaceItem(_sourceIndex, this, _source, value);
        }
        else
        {
            if (Index == -1)
                _groupBy.AddItem(_sourceIndex, this, _source, value);
            else
                _groupBy.ReplaceItem(_sourceIndex, this, _source, value);
        }
    }

    public readonly struct Manager(Func<TSource, IObservable<TKey>> KeySelector) :
        IGroupItemsByStateManager<TKey, TSource, GroupItemsByDynamicState<TKey, TSource>>
    {
        public void Create<TGroupBy>(int sourceIndex, TSource source, ref TGroupBy groupBy, bool replacement)
            where TGroupBy : struct, IGroupItemsByStateMachine<TKey, TSource, GroupItemsByDynamicState<TKey, TSource>>
        {
            var item = new GroupItemsByDynamicState<TKey, TSource>(
                replacement,
                sourceIndex,
                source,
                KeySelector,
                groupBy.Reference);

            if (item.Grouping is null)
            {
                // The selector has an async code inside which hasn't finished yet.
                groupBy.AddItemDeferred(sourceIndex, item);
            }
        }

        public void Dispose(GroupItemsByDynamicState<TKey, TSource> item) =>
            item._subscription.Dispose();

        public void DisposeAll(ReadOnlySpan<GroupItemsByDynamicState<TKey, TSource>> items)
        {
            foreach (var item in items)
                Dispose(item);
        }

        public void SetOriginalIndex(GroupItemsByDynamicState<TKey, TSource> item, int index) =>
            item._sourceIndex = index;

        public void SetOriginalIndexes(ReadOnlySpan<GroupItemsByDynamicState<TKey, TSource>> items, int indexChange)
        {
            foreach (var item in items)
                item._sourceIndex += indexChange;
        }
    }
}