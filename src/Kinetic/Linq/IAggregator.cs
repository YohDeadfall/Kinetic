namespace Kinetic.Linq;

internal interface IAggregator<T, TResult>
{
    static abstract bool RequiresSeed { get; }
    bool Aggregate(T value, ref TResult result);
}