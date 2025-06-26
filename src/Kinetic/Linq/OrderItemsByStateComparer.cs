using System.Collections.Generic;
using System.Diagnostics;

namespace Kinetic.Linq;

internal sealed class OrderItemsByStateComparer<TState, TKey>(IComparer<TKey> Inner) : IComparer<TState>
    where TState : IOrderItemsByState<TKey>
{
    public int Compare(TState? x, TState? y)
    {
        Debug.Assert(x is { });
        Debug.Assert(y is { });
        return Inner.Compare(x.Key, y.Key);
    }
}