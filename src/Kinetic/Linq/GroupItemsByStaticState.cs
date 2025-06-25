using System;

namespace Kinetic.Linq;

internal struct GroupItemsByStaticState : IGroupItemsByState
{
    public int Group { get; set; }
    public int Index { get; set; }

    public readonly struct Manager<TSource, TKey>(Func<TSource, TKey> KeySelector) :
        IGroupItemsByStateManager<GroupItemsByStaticState, TSource, TKey>
    {
        public void Create<TGroupBy>(int sourceIndex, TSource source, ref TGroupBy groupBy, bool replacement)
            where TGroupBy : struct, IGroupItemsByStateMachine<GroupItemsByStaticState, TSource, TKey>
        {
            var key = KeySelector(source);
            var item = new GroupItemsByStaticState();

            if (replacement)
                groupBy.ReplaceItem(sourceIndex, item, source, key);
            else
                groupBy.AddItem(sourceIndex, item, source, key);
        }

        public void Dispose(GroupItemsByStaticState item) { }
        public void DisposeAll(ReadOnlySpan<GroupItemsByStaticState> items) { }

        public void SetOriginalIndex(GroupItemsByStaticState item, int index) { }
        public void SetOriginalIndexes(ReadOnlySpan<GroupItemsByStaticState> items, int indexChange) { }
    }
}