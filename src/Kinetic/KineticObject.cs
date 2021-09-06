using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Kinetic
{
    public abstract class KineticObject
    {
        private PropertyObservable? _observables;
        private uint _suppressions;
        private uint _version;

        private PropertyObservable<T>? GetObservable<T>(IntPtr offset)
        {
            for (var observable = _observables;
                observable is not null;
                observable = observable.Next)
            {
                if (observable.Offset == offset)
                {
                    Debug.Assert(observable is PropertyObservable<T>);
                    return Unsafe.As<PropertyObservable<T>>(observable);
                }
            }

            return null;
        }

        protected void Set<T>(KineticReadOnlyProperty<T> property, T value) =>
            property.EnsureOwner().Set(property.Offset, value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected KineticProperty<T> Property<T>(ref T field)
        {
            var offset = Unsafe.ByteOffset(
                ref GetReference(),
                ref Unsafe.As<T, IntPtr>(ref field));
            return new KineticProperty<T>(this, offset);
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

        internal T Get<T>(IntPtr offset) =>
            GetReference<T>(offset);

        internal void Set<T>(IntPtr offset, T value)
        {
            GetReference<T>(offset) = value;

            var observable = GetObservable<T>(offset);
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

        internal IObservable<T> Changed<T>(IntPtr offset)
        {
            var observable = GetObservable<T>(offset);
            if (observable is null)
            {
                observable = new PropertyObservable<T>(
                    this, offset, next: _observables);

                _observables = observable;
            }

            return observable;
        }

        public SuppressNotificationsScope SuppressNotifications() =>
            new SuppressNotificationsScope(this);

        public readonly struct SuppressNotificationsScope : IDisposable
        {
            private readonly KineticObject? _owner;

            internal SuppressNotificationsScope(KineticObject owner)
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

        private abstract class PropertyObservable
        {
            internal readonly KineticObject Owner;
            internal readonly PropertyObservable? Next;
            internal readonly IntPtr Offset;

            internal uint Version;

            protected PropertyObservable(KineticObject owner, IntPtr offset, PropertyObservable? next) =>
                (Owner, Offset, Next) = (owner, offset, next);

            public abstract void Changed();
        }

        private sealed class PropertyObservable<T> : PropertyObservable, IKineticObservable<T>
        {
            private KineticObservableSubscriptions<T> _subscriptions;

            public PropertyObservable(KineticObject owner, IntPtr offset, PropertyObservable? next)
                : base(owner, offset, next) { }

            public override void Changed() =>
                Changed(Owner.Get<T>(Offset));

            public void Changed(T value) =>
                _subscriptions.OnNext(value);

            public IDisposable Subscribe(IObserver<T> observer) =>
                _subscriptions.Subscribe(this, observer, Owner.Get<T>(Offset));

            public void Subscribe(KineticObservableSubscription<T> subscription) =>
                _subscriptions.Subscribe(this, subscription);

            public void Unsubscribe(KineticObservableSubscription<T> subscription) =>
                _subscriptions.Unsubscribe(subscription);
        }
    }

    public readonly ref struct KineticProperty<T>
    {
        internal readonly KineticObject? Owner;
        internal readonly IntPtr Offset;

        internal KineticProperty(KineticObject? owner, IntPtr offset) =>
            (Owner, Offset) = (owner, offset);

        internal KineticObject EnsureOwner() =>
            Owner ?? throw new InvalidOperationException();

        public T Get() => EnsureOwner().Get<T>(Offset);

        public void Set(T value) => EnsureOwner().Set(Offset, value);

        public IObservable<T> Changed => EnsureOwner().Changed<T>(Offset);

        public static implicit operator T(KineticProperty<T> property) =>
            property.Get();

        public static implicit operator KineticReadOnlyProperty<T>(KineticProperty<T> property) =>
            new KineticReadOnlyProperty<T>(property.Owner, property.Offset);
    }

    public readonly ref struct KineticReadOnlyProperty<T>
    {
        internal readonly KineticObject? Owner;
        internal readonly IntPtr Offset;

        internal KineticReadOnlyProperty(KineticObject? owner, IntPtr offset) =>
            (Owner, Offset) = (owner, offset);

        internal KineticObject EnsureOwner() =>
            Owner ?? throw new InvalidOperationException();

        public T Get() => EnsureOwner().Get<T>(Offset);

        public IObservable<T> Changed => EnsureOwner().Changed<T>(Offset);

        public static implicit operator T(KineticReadOnlyProperty<T> property) =>
            property.Get();
    }
}