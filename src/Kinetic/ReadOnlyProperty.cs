using System;
using System.Diagnostics;

namespace Kinetic;

[DebuggerDisplay("{Get()}")]
[DebuggerTypeProxy(typeof(ReadOnlyPropertyDebugView<>))]
public readonly struct ReadOnlyProperty<T>
{
    internal readonly ObservableObject Owner;
    internal readonly IntPtr Offset;

    internal ReadOnlyProperty(ObservableObject owner, IntPtr offset) =>
        (Owner, Offset) = (owner, offset);

    public T Get() =>
        Owner.Get<T>(Offset);

    public IObservable<T> Changed =>
        Owner.EnsureObservableFor<T>(Offset);

    public static implicit operator T(ReadOnlyProperty<T> property) =>
        property.Get();
}