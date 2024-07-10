using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Kinetic;

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

    private protected PropertyObservable EnsureObservable(IntPtr offset, Func<ObservableObject, IntPtr, PropertyObservable?, PropertyObservable> factory)
    {
        var observable = GetObservable(offset);
        if (observable is null)
        {
            observable = factory(this, offset, _observables);

            _observables = observable;
        }

        return observable;
    }

    internal PropertyObservable<T> EnsureObservableFor<T>(IntPtr offset)
    {
        var observable = GetObservableFor<T>(offset);
        if (observable is null)
        {
            observable = new PropertyObservable<T>(
                this, offset, next: _observables);

            _observables = observable;
        }

        return observable;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void Set<T>(ReadOnlyProperty<T> property, T value) =>
        property.Owner.Set(property.Offset, value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected Property<T> Property<T>(ref T field)
    {
        var offset = GetOffsetOf(ref field);
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
        if (EqualityComparer<T>.Default.Equals(value, Get<T>(offset)))
        {
            return;
        }

        GetReference<T>(offset) = value;

        var observable = GetObservableFor<T>(offset);
        if (observable is not null)
        {
            if (_suppressions > 0)
            {
                observable.Version = _version;
            }
            else
            {
                observable.Version = _version++;
                observable.Changed(value);
            }
        }
    }

    public SuppressNotificationsScope SuppressNotifications() =>
        new SuppressNotificationsScope(this);

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
}