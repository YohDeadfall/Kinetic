using System;
using System.Threading;
using System.Threading.Tasks;

namespace Kinetic.Linq;

public static class Observable
{
    public static Operator<FromAsync<T>, T> FromAsync<T>(Func<CancellationToken, ValueTask<T>> task, bool configureAwait = true)
    {
        return new(new(task, configureAwait));
    }
}