namespace Kinetic.Linq;

internal readonly struct FirstAggregator<T> : IAggregator<T, T>
{
    public static bool RequiresSeed => false;

    public bool Aggregate(T value, ref T result)
    {
        result = value;
        return false;
    }
}