using System;

namespace Kinetic.Linq;

internal sealed class GroupItemsByDynamicState<TSource, TKey> : IGroupItemsByState, IObserver<TKey>
{
    private int _sourceIndex;
    private readonly TSource _source;
    private readonly IDisposable _subscription;
    private readonly IGroupItemsByStateMachine<GroupItemsByDynamicState<TSource, TKey>, TSource, TKey> _groupBy;

    public int Group { get; set; }
    public int Index { get; set; }

    private GroupItemsByDynamicState(
        bool replacement,
        int sourceIndex,
        TSource source,
        Func<TSource, IObservable<TKey>> keySelector,
        IGroupItemsByStateMachine<GroupItemsByDynamicState<TSource, TKey>, TSource, TKey> groupBy)
    {
        Group = -1;
        Index = replacement ? 0 : -1;

        _source = source;
        _groupBy = groupBy;
        _subscription = keySelector(source).Subscribe(this);
    }

    public void OnCompleted()
    {
        throw new NotImplementedException();
    }

    public void OnError(Exception error)
    {
        throw new NotImplementedException();
    }

    public void OnNext(TKey value)
    {
        // If there's no key changed subscription then it might be possible
        // that the item was disposed while waiting to be processed. So,
        // an additional check is required to see if the item was used.
        if (_sourceIndex == -1)
            return;

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
        IGroupItemsByStateManager<GroupItemsByDynamicState<TSource, TKey>, TSource, TKey>
    {
        public void Create<TGroupBy>(int sourceIndex, TSource source, ref TGroupBy groupBy, bool replacement)
            where TGroupBy : struct, IGroupItemsByStateMachine<GroupItemsByDynamicState<TSource, TKey>, TSource, TKey>
        {
            var item = new GroupItemsByDynamicState<TSource, TKey>(
                replacement,
                sourceIndex,
                source,
                KeySelector,
                groupBy.Reference);

            if (item.Group == -1)
            {
                // The selector has an async code inside which hasn't finished yet.
                groupBy.AddItemDeferred(sourceIndex, item);
            }
        }

        public void Dispose(GroupItemsByDynamicState<TSource, TKey> item) =>
            item._subscription.Dispose();

        public void DisposeAll(ReadOnlySpan<GroupItemsByDynamicState<TSource, TKey>> items)
        {
            foreach (var item in items)
                Dispose(item);
        }

        public void SetOriginalIndex(GroupItemsByDynamicState<TSource, TKey> item, int index) =>
            item._sourceIndex = index;

        public void SetOriginalIndexes(ReadOnlySpan<GroupItemsByDynamicState<TSource, TKey>> items, int indexChange)
        {
            foreach (var item in items)
                item._sourceIndex += indexChange;
        }
    }
}