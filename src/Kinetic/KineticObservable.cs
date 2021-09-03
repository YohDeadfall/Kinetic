using System;

namespace Kinetic
{
    public abstract class KineticObservable<T> : IObservable<T>
    {
        private Subscription? _subscriptions;
        
        protected void OnNext(T value)
        {
            var subscriptions = _subscriptions;
            var subscription = subscriptions;

            while (subscription is not null)
            {
                subscription.OnNext(value);
                subscription = subscription.Next;
            }
        }

        protected void OnError(Exception error)
        {
            var subscriptions = _subscriptions;
            var subscription = subscriptions;

            while (subscription is not null)
            {
                subscription.OnError(error);
                subscription = subscription.Next;
            }
        }

        protected void OnCompleted()
        {
            var subscriptions = _subscriptions;
            var subscription = subscriptions;

            while (subscription is not null)
            {
                subscription.OnCompleted();
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

            return subscription;
        }

        private sealed class Subscription : IDisposable
        {
            internal Subscription? Previous;
            internal Subscription? Next;

            private readonly KineticObservable<T> _observable;
            private readonly IObserver<T> _observer;

            public Subscription(KineticObservable<T> observable, IObserver<T> observer) =>
                (_observable, _observer) = (observable, observer);

            public void OnNext(T value) =>
                _observer.OnNext(value);

            public void OnError(Exception error) =>
                _observable.OnError(error);

            public void OnCompleted() =>
                _observable.OnCompleted();

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
