using System;

namespace Kinetic.Linq;

internal readonly struct FuncAggregator<T, TResult> : IAggregator<T, TResult>
{
    private readonly Func<TResult, T, TResult> _accumulator;

    public FuncAggregator(Func<TResult, T, TResult> accumulator) =>
        _accumulator = accumulator.ThrowIfNull();

    public static bool RequiresSeed => throw new NotImplementedException();

    public bool Aggregate(T value, ref TResult result)
    {
        throw new NotImplementedException();
    }
}