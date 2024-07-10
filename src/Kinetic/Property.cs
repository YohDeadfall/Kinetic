using System;
using System.Diagnostics;

namespace Kinetic;

[DebuggerDisplay("Get()")]
[DebuggerTypeProxy(typeof(PropertyDebugView<>))]
public readonly struct Property<T>
{
    internal readonly ObservableObject Owner;
    internal readonly IntPtr Offset;

    internal Property(ObservableObject owner, IntPtr offset) =>
        (Owner, Offset) = (owner, offset);

    public T Get() =>
        Owner.Get<T>(Offset);

    public void Set(T value) =>
        Owner.Set(Offset, value);

    public IObservable<T> Changed =>
        Owner.EnsureObservableFor<T>(Offset);

    public static implicit operator T(Property<T> property) =>
        property.Get();

    public static implicit operator ReadOnlyProperty<T>(Property<T> property) =>
        new ReadOnlyProperty<T>(property.Owner, property.Offset);
}