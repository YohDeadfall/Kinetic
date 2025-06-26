namespace Kinetic.Linq;

internal interface IOrderItemsByState<TKey>
{
    int Index { get; set; }
    TKey Key { get; }
}