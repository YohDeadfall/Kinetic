using System;

namespace Kinetic
{
    internal sealed class KineticPropertyObservable<T> : KineticPropertyObservable, IKineticObservable<T>
    {
        private KineticObservableSubscriptions<T> _subscriptions;

        public KineticPropertyObservable(KineticObject owner, IntPtr offset, KineticPropertyObservable? next)
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