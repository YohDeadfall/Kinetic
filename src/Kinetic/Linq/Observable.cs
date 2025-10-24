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

    public static Operator<Return<T>, T> Return<T>(T value) =>
        new(new(value));

    public static Operator<ReturnAt<T>, T> Return<T>(T value, TimeSpan dueTime) =>
        new(new(value, dueTime, TimeProvider.System, CancellationToken.None));

    public static Operator<ReturnAt<T>, T> Return<T>(T value, TimeSpan dueTime, CancellationToken cancellationToken) =>
        new(new(value, dueTime, TimeProvider.System, cancellationToken));

    public static Operator<ReturnAt<T>, T> Return<T>(T value, TimeSpan dueTime, TimeProvider timeProvider) =>
        new(new(value, dueTime, timeProvider, CancellationToken.None));

    public static Operator<ReturnAt<T>, T> Return<T>(T value, TimeSpan dueTime, TimeProvider timeProvider, CancellationToken cancellationToken) =>
        new(new(value, dueTime, timeProvider, cancellationToken));

    public static Operator<WhenAny<T1, T2>, (T1, T2)> WhenAnyValue<T1, T2>(
        ReadOnlyProperty<T1> source1,
        ReadOnlyProperty<T2> source2)
    {
        return new(new(source1, source2));
    }

    public static Operator<WhenAny<T1, T2, T3>, (T1, T2, T3)> WhenAnyValue<T1, T2, T3>(
        ReadOnlyProperty<T1> source1,
        ReadOnlyProperty<T2> source2,
        ReadOnlyProperty<T3> source3)
    {
        return new(new(source1, source2, source3));
    }

    public static Operator<WhenAny<T1, T2, T3, T4>, (T1, T2, T3, T4)> WhenAnyValue<T1, T2, T3, T4>(
        ReadOnlyProperty<T1> source1,
        ReadOnlyProperty<T2> source2,
        ReadOnlyProperty<T3> source3,
        ReadOnlyProperty<T4> source4)
    {
        return new(new(source1, source2, source3, source4));
    }

    public static Operator<WhenAny<T1, T2>, (T1, T2)> WhenAnyValue<T, T1, T2>(
        this T obj,
        Func<T, Property<T1>> source1,
        Func<T, Property<T2>> source2)
    {
        return new(new(source1(obj), source2(obj)));
    }

    public static Operator<WhenAny<T1, T2, T3>, (T1, T2, T3)> WhenAnyValue<T, T1, T2, T3>(
        this T obj,
        Func<T, Property<T1>> source1,
        Func<T, Property<T2>> source2,
        Func<T, Property<T3>> source3)
    {
        return new(new(source1(obj), source2(obj), source3(obj)));
    }

    public static Operator<WhenAny<T1, T2, T3, T4>, (T1, T2, T3, T4)> WhenAnyValue<T, T1, T2, T3, T4>(
        this T obj,
        Func<T, Property<T1>> source1,
        Func<T, Property<T2>> source2,
        Func<T, Property<T3>> source3,
        Func<T, Property<T4>> source4)
    {
        return new(new(source1(obj), source2(obj), source3(obj), source4(obj)));
    }
}