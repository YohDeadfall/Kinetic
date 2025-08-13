using System.Collections.Generic;

namespace Kinetic.Linq;

internal readonly struct ContainsAggregator<T> : IAggregator<T, bool>
{
    private readonly T _value;
    private readonly IEqualityComparer<T>? _comparer;

    public ContainsAggregator(T value, IEqualityComparer<T>? comparer)
    {
        _value = value;
        _comparer = comparer;
    }

    public static bool RequiresSeed => false;

    public bool Aggregate(T value, ref bool result)
    {
        var equals = _comparer?.Equals(value, _value)
            ?? EqualityComparer<T>.Default.Equals(value, _value);
        if (equals)
        {
            result = true;
            return false;
        }

        return true;
    }
}