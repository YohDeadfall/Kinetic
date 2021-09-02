using System;

namespace Kinetic
{
    internal sealed class KineticPropertyObservable<T> : KineticPropertyObservable, IObservable<T>
    {
        private Subscription? _subscriptions;

        public KineticPropertyObservable(KineticObject owner, IntPtr offset, KineticPropertyObservable? next)
            : base(owner, offset, next) { }

        public override void Changed()
        {
            Changed(Owner.Get<T>(Offset));
        }

        public void Changed(T value)
        {
            var subscriptions = _subscriptions;
            var subscription = subscriptions;

            while (subscription is not null)
            {
                subscription.Changed(value);
                subscription = subscription.Next;
            }
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            var subscription = new Subscription(
                observable: this,
                observer ?? throw new ArgumentNullException(nameof(observer)));

            var subscriptions = _subscriptions;
            if (subscriptions is not null)
            {
                subscription.Next = subscriptions;
                subscriptions.Previous = subscription;
            }

            _subscriptions = subscription;

            observer.OnNext(Owner.Get<T>(Offset));
            return subscription;
        }

        private sealed class Subscription : IDisposable
        {
            internal Subscription? Previous;
            internal Subscription? Next;

            private readonly KineticPropertyObservable<T> _observable;
            private readonly IObserver<T> _observer;

            public Subscription(KineticPropertyObservable<T> observable, IObserver<T> observer) =>
                (_observable, _observer) = (observable, observer);

            public void Changed(T value) =>
                _observer.OnNext(value);

            public void Dispose()
            {
                if (_observable is { } observable &&
                    _observer is { } observer)
                {
                    if (Previous is { } previous)
                    {
                        previous.Next = Next;
                    }
                    else
                    {
                        observable._subscriptions = Next;
                    }

                    if (Next is { } next)
                    {
                        next.Previous = Previous;
                    }
                }
            }
        }
    }
}
