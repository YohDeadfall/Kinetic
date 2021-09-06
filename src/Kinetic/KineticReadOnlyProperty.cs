using System;

namespace Kinetic
{
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