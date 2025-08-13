using System.Collections.Generic;
using System.Diagnostics;

namespace Kinetic.Linq;

internal sealed class OrderItemsByStateComparer<TState, TKey>(IComparer<TKey>? inner) : IComparer<TState>
    where TState : IOrderItemsByState<TKey>
{
    private readonly IComparer<TKey>? _inner = typeof(TKey).IsValueType
        ? inner is { } && inner == Comparer<TKey>.Default ? null : inner
        : inner ?? Comparer<TKey>.Default;

    public int Compare(TState? x, TState? y)
    {
        Debug.Assert(x is { });
        Debug.Assert(y is { });
        return typeof(TKey).IsValueType && _inner is null
            ? Comparer<TKey>.Default.Compare(x.Key, y.Key)
            : _inner!.Compare(x.Key, y.Key);
    }
}