using System;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace Kinetic.Linq;

public static class Observable
{
    public static Operator<FromGenerator<T>, T> Create<T>(Func<IObserver<T>, IDisposable> subscribe) =>
        new(new(subscribe));

    public static Operator<FromGeneratorWithCancellation<T>, T> Create<T>(Func<IObserver<T>, CancellationToken, IDisposable> subscribe) =>
        new(new(subscribe));

    public static Operator<FromAsync, ValueTuple> FromAsync(Func<CancellationToken, ValueTask> task, bool configureAwait = true) =>
        new(new(task, configureAwait));

    public static Operator<FromAsync<T>, T> FromAsync<T>(Func<CancellationToken, ValueTask<T>> task, bool configureAwait = true) =>
        new(new(task, configureAwait));

    public static Operator<FromRange<T>, T> FromRange<T>(T start, T end)
        where T : INumberBase<T>, IComparisonOperators<T, T, bool>
    {
        return FromRange(start, end, T.One);
    }

    public static Operator<FromRange<T>, T> FromRange<T>(T start, T end, T step)
        where T : IAdditionOperators<T, T, T>, IComparisonOperators<T, T, bool>
    {
        return new(new(start, end, step, inclusive: false));
    }

    public static Operator<FromRange<T>, T> FromRangeInclusive<T>(T start, T end)
        where T : INumberBase<T>, IComparisonOperators<T, T, bool>
    {
        return FromRangeInclusive(start, end, T.One);
    }

    public static Operator<FromRange<T>, T> FromRangeInclusive<T>(T start, T end, T step)
        where T : IAdditionOperators<T, T, T>, IComparisonOperators<T, T, bool>
    {
        return new(new(start, end, step, inclusive: false));
    }

    public static Operator<Never<T>, T> Never<T>() =>
        new(new());
}