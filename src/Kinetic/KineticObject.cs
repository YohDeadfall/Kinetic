using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Kinetic
{
    public abstract class KineticObject
    {
        private KineticPropertyObservable? _observables;
        private uint _suppressions;
        private uint _version;

        private KineticPropertyObservable<T>? GetObservable<T>(IntPtr offset)
        {
            for (var observable = _observables;
                observable is not null;
                observable = observable.Next)
            {
                if (observable.Offset == offset)
                {
                    Debug.Assert(observable is KineticPropertyObservable<T>);
                    return Unsafe.As<KineticPropertyObservable<T>>(observable);
                }
            }

            return null;
        }

        protected void Set<T>(KineticReadOnlyProperty<T> property, T value) =>
            property.EnsureOwner().Set(property.Offset, value);

        protected KineticProperty<T> Property<T>(ref T field)
        {
            var offset = Unsafe.ByteOffset(
                ref GetReference(),
                ref Unsafe.As<T, IntPtr>(ref field));
            return new KineticProperty<T>(this, offset);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ref IntPtr GetReference() =>
            ref Unsafe.As<KineticPropertyObservable?, IntPtr>(ref _observables);

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
                observable = new KineticPropertyObservable<T>(
                    this, offset, next: _observables);

                _observables = observable;
            }

            return observable;
        }

        public SuppressNotificationsScope SuppressNotifications() =>
            new SuppressNotificationsScope(this);

        public readonly struct SuppressNotificationsScope
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
    }
}
