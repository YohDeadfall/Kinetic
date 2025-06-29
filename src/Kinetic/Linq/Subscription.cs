using System;

namespace Kinetic.Linq;

internal sealed class Subscription : IDisposable
{
    public static readonly Subscription Cold = new();

    public void Dispose() { }
}
