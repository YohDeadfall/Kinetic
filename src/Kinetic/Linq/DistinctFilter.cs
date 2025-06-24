using System.Collections.Generic;

namespace Kinetic.Linq;

internal readonly struct DistinctFilter<T> : ITransform<T, bool>
{
    private readonly HashSet<T> _set;

    public DistinctFilter(IEqualityComparer<T>? comparer) =>
        _set = new(comparer);

    public bool Transform(T value) =>
        _set.Add(value);
}