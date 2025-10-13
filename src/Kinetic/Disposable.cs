using System;

namespace Kinetic.Linq;

public static class Disposable
{
    public static IDisposable Empty { get; } = new EmptyDisposable();

}