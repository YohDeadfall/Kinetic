using System;

namespace Kinetic;

internal abstract class PropertyObservable
{
    internal readonly IntPtr Offset;
    internal readonly ObservableObject Owner;
    internal readonly PropertyObservable? Next;

    internal uint Version;

    protected PropertyObservable(IntPtr offset, ObservableObject owner, PropertyObservable? next) =>
        (Owner, Offset, Next) = (owner, offset, next);

    internal abstract void Changed();
}