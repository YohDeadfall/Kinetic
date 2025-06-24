using System;
using System.Collections.Generic;

namespace Kinetic.Linq;

internal readonly struct DistinctByFilter<T, TKey> : ITransform<T, bool>
{
    private readonly Func<T, TKey> _keySelector;
    private readonly HashSet<TKey> _set;

    public DistinctByFilter(Func<T, TKey> keySelector, IEqualityComparer<TKey>? comparer)
    {
        _keySelector = keySelector;
        _set = new(comparer);
    }

    public bool Transform(T value) =>
        _set.Add(_keySelector(value));
}