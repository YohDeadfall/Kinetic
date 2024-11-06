using System;
using System.Diagnostics;

namespace Kinetic;

/// <summary>
/// A read-only property of an observable object.
/// </summary>
[DebuggerDisplay("{Get()}")]
[DebuggerTypeProxy(typeof(ReadOnlyPropertyDebugView<>))]
public readonly struct ReadOnlyProperty<T>
{
    internal readonly ObservableObject Owner;
    internal readonly IntPtr Offset;

    internal ReadOnlyProperty(ObservableObject owner, IntPtr offset) =>
        (Owner, Offset) = (owner, offset);

    /// <summary>
    /// Gets a value of this property.
    /// </summary>
    /// <returns>A value of this property.</returns>
    public T Get() =>
        Owner.Get<T>(Offset);

    /// <summary>
    /// Gets an <see cref="IObservable{T}"/> notifying about changes of this property.
    /// </summary>
    /// <returns>
    /// An <see cref="IObservable{T}"/> notifying about changes of this property.
    /// </returns>
    public IObservable<T> Changed =>
        Owner.EnsureObservableFor<T>(Offset);

    /// <summary>
    /// Defines an implicit cast of a <see cref="Property{T}"/> to its value.
    /// </summary>
    /// <param name="property">The property which value to be returned.</param>
    /// <returns>A value of the property.</returns>
    public static implicit operator T(ReadOnlyProperty<T> property) =>
        property.Get();
}