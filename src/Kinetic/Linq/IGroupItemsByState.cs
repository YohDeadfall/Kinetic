namespace Kinetic.Linq;

internal interface IGroupItemsByState<TKey, TSource>
{
    ListGrouping<TKey, TSource>? Grouping { get; set; }
    int Index { get; set; }
}