using System;
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
}