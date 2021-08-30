using System;

namespace Kinetic
{
    internal abstract class KineticPropertyObservable
    {
        internal readonly KineticObject Owner;
        internal readonly KineticPropertyObservable? Next;
        internal readonly IntPtr Offset;

        internal uint Version;

        protected KineticPropertyObservable(KineticObject owner, IntPtr offset, KineticPropertyObservable? next) =>
            (Owner, Offset, Next) = (owner, offset, next);

        public abstract void Changed();
    }
}