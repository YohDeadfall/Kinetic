using System;
using System.Diagnostics;

namespace Kinetic;

/// <summary>
/// A property of an observable object.
/// </summary>
[DebuggerDisplay("{Get()}")]
[DebuggerTypeProxy(typeof(PropertyDebugView<>))]
public readonly struct Property<T>
{
    internal readonly ObservableObject Owner;
    internal readonly IntPtr Offset;

    internal Property(ObservableObject owner, IntPtr offset) =>
        (Owner, Offset) = (owner, offset);

    /// <summary>
    /// Gets a value of this property.
    /// </summary>
    /// <returns>A value of this property.</returns>
    public T Get() =>
        Owner.Get<T>(Offset);

    /// <summary>
    /// Sets a value for this property.
    /// </summary>
    /// <param name="value">The value to be set.</param>
    public void Set(T value) =>
        Owner.Set(Offset, value);

    /// <summary>
    /// Gets an <see cref="IObserer{T}"/> setting a value of this property.
    /// </summary>
    /// <returns>
    /// An <see cref="IObservable{T}"/> notifying about changes of this property.
    /// </returns>
    public IObserver<T> Change =>
        Owner.EnsureObservableFor<T>(Offset);

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
    public static implicit operator T(Property<T> property) =>
        property.Get();

    /// <summary>
    /// Defines an implicit cast of a <see cref="Property{T}"/>
    /// to a <see cref="ReadOnlyProperty{T}"/>.
    /// </summary>
    /// <param name="property">The property to be converted to <see cref="ReadOnlyProperty{T}"/>.</param>
    /// <returns>A read only version of the property.</returns>
    public static implicit operator ReadOnlyProperty<T>(Property<T> property) =>
        new ReadOnlyProperty<T>(property.Owner, property.Offset);
}