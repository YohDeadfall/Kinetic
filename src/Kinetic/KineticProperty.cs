using System;

namespace Kinetic
{
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
}