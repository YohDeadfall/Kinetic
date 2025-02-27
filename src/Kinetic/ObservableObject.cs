using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Kinetic.Linq.StateMachines;

namespace Kinetic;

/// <summary>
/// An object with observable properties.
/// </summary>
public abstract class ObservableObject
{
    private PropertyObservable? _observables;
    private uint _suppressions;
    private uint _version;

    protected bool NotificationsEnabled => _suppressions == 0;

    private protected PropertyObservable? GetObservable(IntPtr offset)
    {
        for (var observable = _observables;
            observable is not null;
            observable = observable.Next)
        {
            if (observable.Offset == offset)
            {
                return observable;
            }
        }

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal PropertyObservable<T>? GetObservableFor<T>(IntPtr offset)
    {
        var observable = GetObservable(offset);

        Debug.Assert(
            observable is null ||
            observable is PropertyObservable<T>);

        return Unsafe.As<PropertyObservable<T>>(observable);
    }

    private protected PropertyObservable EnsureObservable(IntPtr offset, Func<ObservableObject, IntPtr, PropertyObservable?, PropertyObservable> factory) =>
        GetObservable(offset) ?? (_observables = factory(this, offset, _observables));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal PropertyObservable<T> EnsureObservableFor<T>(IntPtr offset) =>
        Unsafe.As<PropertyObservable<T>>(EnsureObservable(offset, (owner, offset, next) => new PropertyObservable<T, SetValueStateMachine<T>>(offset, owner, next, default)));

    /// <summary>
    /// Sets a value of the specified property and notifies the observers.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="property">The property which value will be set.</param>
    /// <param name="value">The value to be set.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void Set<T>(ReadOnlyProperty<T> property, T value)
    {
        if (property.Owner != this)
        {
            throw new ArgumentException("The property belongs to a different object.", nameof(property));
        }

        property.Owner.Set(property.Offset, value);
    }

    /// <summary>
    /// Creates an observable property for the specified field.
    /// </summary>
    /// <typeparam name="T">The type of the field.</typeparam>
    /// <param name="field">The field for which an observable propery will be created.</param>
    /// <returns>Returns an observable property for the specified field.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected Property<T> Property<T>(ref T field)
    {
        var offset = GetOffsetOf(ref field);
        return new Property<T>(this, offset);
    }

    /// <summary>
    /// Creates an observable property for the specified field with a value changing handler.
    /// </summary>
    /// <typeparam name="T">The type of the field.</typeparam>
    /// <param name="field">The field for which an observable propery will be created.</param>
    /// <param name="changing">The value changing handler that is invoked before setting a new value.</param>
    /// <returns>Returns an observable property for the specified field.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected Property<T> Property<T>(ref T field, Func<ObserverBuilder<T>, ObserverBuilder<T>> changing)
    {
        var offset = GetOffsetOf(ref field);
        var observable = GetObservableFor<T>(offset);
        if (observable is null)
        {
            EnsureObservable(offset, (owner, offset, next) => changing(default)
                .Build<SetValueStateMachine<T>, PropertyObservableFactory, PropertyObservable>(new(), new(offset, owner, next)));
        }

        return new Property<T>(this, offset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private protected IntPtr GetOffsetOf<T>(ref T field)
    {
        return Unsafe.ByteOffset(
            ref GetReference(),
            ref Unsafe.As<T, IntPtr>(ref field));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ref IntPtr GetReference() =>
        ref Unsafe.As<PropertyObservable?, IntPtr>(ref _observables);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ref T GetReference<T>(IntPtr offset)
    {
        ref var baseRef = ref GetReference();
        ref var valueRef = ref Unsafe.AddByteOffset(ref baseRef, offset);
        return ref Unsafe.As<IntPtr, T>(ref valueRef);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal T Get<T>(IntPtr offset) =>
        GetReference<T>(offset);

    internal void Set<T>(IntPtr offset, T value)
    {
        var observable = GetObservableFor<T>(offset);
        if (observable is { })
        {
            observable.Changing(value);
        }
        else
        {
            GetReference<T>(offset) = value;
        }
    }

    private static void SetCore<T>(PropertyObservable<T> observable, T value)
    {
        var owner = observable.Owner;

        if (EqualityComparer<T>.Default.Equals(value, owner.Get<T>(observable.Offset)))
        {
            return;
        }

        owner.GetReference<T>(observable.Offset) = value;

        if (observable.Owner.NotificationsEnabled)
        {
            observable.Version = owner._version++;
            observable.Changed(value);
        }
        else
        {
            observable.Version = owner._version;
        }
    }

    /// <summary>
    /// Suppresses notifications for the current object and returns a
    /// <see cref="SuppressNotificationsScope"/> controlling the time
    /// for which notifications are disabled.
    /// </summary>
    /// <returns>An object serving as a scope of the notification suppression.</returns>
    public SuppressNotificationsScope SuppressNotifications() =>
        new SuppressNotificationsScope(this);

    /// <summary>
    /// A scope controlling the time during notifications are disabled for the object
    /// for which the <see cref="SuppressNotifications"/> method was called.
    /// <summary>
    public readonly struct SuppressNotificationsScope : IDisposable
    {
        private readonly ObservableObject? _owner;

        internal SuppressNotificationsScope(ObservableObject owner)
        {
            if (owner._suppressions == 0)
            {
                owner._version += 1;
            }

            _owner = owner;
            _owner._suppressions++;
        }

        /// <summary>
        /// Enables notifications for the object for which they were previously
        /// suppressed by a call to the <see cref="SuppressNotifications"/> method.
        public void Dispose()
        {
            if (_owner is not null &&
                _owner._suppressions-- == 1)
            {
                var version = _owner._version;
                for (var observable = _owner._observables;
                    observable is not null;
                    observable = observable.Next)
                {
                    if (observable.Version == version)
                    {
                        observable.Changed();
                    }
                }
            }
        }
    }

    private struct SetValueStateMachine<T> : IStateMachine<T>
    {
        private PropertyObservable<T>? _observable;

        public StateMachineBox Box =>
            _observable ?? throw new InvalidOperationException();

        public StateMachine<T> Reference =>
            new StateMachine<T, SetValueStateMachine<T>>(ref this);

        public StateMachine? Continuation =>
            null;

        public void Dispose() =>
            _observable = null;

        public void Initialize(StateMachineBox box) =>
            _observable = (PropertyObservable<T>) box;

        public void OnCompleted() { }

        public void OnError(Exception error) { }

        public void OnNext(T value) =>
            SetCore(_observable!, value);
    }
}