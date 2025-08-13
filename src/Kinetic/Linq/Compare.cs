using System.Collections.Generic;

namespace Kinetic.Linq;

internal readonly struct Compare<T, TOrdering> : IAggregator<T, T>
    where TOrdering : IOrdering
{
    private readonly IComparer<T>? _comparer;

    public Compare(IComparer<T>? comparer) =>
        _comparer = comparer;

    public static bool RequiresSeed => true;

    public bool Aggregate(T value, ref T result)
    {
        var ordering = _comparer?.Compare(value, result)
            ?? Comparer<T>.Default.Compare(value, result);

        if (TOrdering.Matches(ordering))
            result = value;

        return true;
    }
}