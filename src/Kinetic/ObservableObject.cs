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
    private PropertyObservable<T>? GetObservableFor<T>(IntPtr offset)
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

    internal IObservable<T> EnsureObservableFor<T>(IntPtr offset)
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

public readonly struct Property<T>
{
    internal readonly ObservableObject Owner;
    internal readonly IntPtr Offset;

    internal Property(ObservableObject owner, IntPtr offset) =>
        (Owner, Offset) = (owner, offset);

    public T Get() => Owner.Get<T>(Offset);

    public void Set(T value) => Owner.Set(Offset, value);

    public IObservable<T> Changed => Owner.EnsureObservableFor<T>(Offset);

    public static implicit operator T(Property<T> property) =>
        property.Get();

    public static implicit operator ReadOnlyProperty<T>(Property<T> property) =>
        new ReadOnlyProperty<T>(property.Owner, property.Offset);
}

public readonly struct ReadOnlyProperty<T>
{
    internal readonly ObservableObject Owner;
    internal readonly IntPtr Offset;

    internal ReadOnlyProperty(ObservableObject owner, IntPtr offset) =>
        (Owner, Offset) = (owner, offset);

    public T Get() => Owner.Get<T>(Offset);

    public IObservable<T> Changed => Owner.EnsureObservableFor<T>(Offset);

    public static implicit operator T(ReadOnlyProperty<T> property) =>
        property.Get();
}

internal abstract class PropertyObservable
{
    internal readonly ObservableObject Owner;
    internal readonly PropertyObservable? Next;
    internal readonly IntPtr Offset;

    internal uint Version;

    protected PropertyObservable(ObservableObject owner, IntPtr offset, PropertyObservable? next) =>
        (Owner, Offset, Next) = (owner, offset, next);

    public abstract void Changed();
}

internal sealed class PropertyObservable<T> : PropertyObservable, IObservableInternal<T>
{
    private ObservableSubscriptions<T> _subscriptions;

    public PropertyObservable(ObservableObject owner, IntPtr offset, PropertyObservable? next)
        : base(owner, offset, next) { }

    public override void Changed() =>
        Changed(Owner.Get<T>(Offset));

    public void Changed(T value) =>
        _subscriptions.OnNext(value);

    public IDisposable Subscribe(IObserver<T> observer)
    {
        observer.OnNext(Owner.Get<T>(Offset));
        return _subscriptions.Subscribe(this, observer);
    }

    public void Subscribe(ObservableSubscription<T> subscription) =>
        _subscriptions.Subscribe(this, subscription);

    public void Unsubscribe(ObservableSubscription<T> subscription) =>
        _subscriptions.Unsubscribe(subscription);
}