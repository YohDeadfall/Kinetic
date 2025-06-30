using System;

namespace Kinetic.Linq;

internal struct GroupItemsByStaticState<TKey, TSource> : IGroupItemsByState<TKey, TSource>
{
    public ListGrouping<TKey, TSource>? Grouping { get; set; }
    public int Index { get; set; }

    public readonly struct Manager(Func<TSource, TKey> KeySelector) :
        IGroupItemsByStateManager<TKey, TSource, GroupItemsByStaticState<TKey, TSource>>
    {
        public void Create<TGroupBy>(int sourceIndex, TSource source, ref TGroupBy groupBy, bool replacement)
            where TGroupBy : struct, IGroupItemsByStateMachine<TKey, TSource, GroupItemsByStaticState<TKey, TSource>>
        {
            var key = KeySelector(source);
            var item = new GroupItemsByStaticState<TKey, TSource>();

            if (replacement)
                groupBy.ReplaceItem(sourceIndex, item, source, key);
            else
                groupBy.AddItem(sourceIndex, item, source, key);
        }

        public void Dispose(GroupItemsByStaticState<TKey, TSource> item) { }
        public void DisposeAll(ReadOnlySpan<GroupItemsByStaticState<TKey, TSource>> items) { }

        public void SetOriginalIndex(GroupItemsByStaticState<TKey, TSource> item, int index) { }
        public void SetOriginalIndexes(ReadOnlySpan<GroupItemsByStaticState<TKey, TSource>> items, int indexChange) { }
    }
}