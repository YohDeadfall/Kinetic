using System;

namespace Kinetic.Linq;

internal sealed class EmptyDisposable : IDisposable
{
    public void Dispose()
    {
    }
}