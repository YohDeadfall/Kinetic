using System;

namespace Kinetic
{
    internal interface IObservableInternal<T> : IObservable<T>
    {
        void Subscribe(ObservableSubscription<T> subscription);
        void Unsubscribe(ObservableSubscription<T> subscription);
    }

    internal sealed class ObservableSubscription<T> : IDisposable
    {
        internal IObservableInternal<T>? Observable;
        internal ObservableSubscription<T>? Next;

        private readonly IObserver<T> _observer;

        public ObservableSubscription(IObserver<T> observer) => _observer = observer;

        public void Dispose() => Observable?.Unsubscribe(this);

        public void OnNext(T value) => _observer.OnNext(value);
        public void OnError(Exception error) => _observer.OnError(error);
        public void OnCompleted() => _observer.OnCompleted();
    }

    internal struct ObservableSubscriptions<T>
    {
        private ObservableSubscription<T>? _head;

        public IDisposable Subscribe(IObservableInternal<T> observable, IObserver<T> observer, T value)
        {
            var subscription = new ObservableSubscription<T>(observer);

            Subscribe(observable, subscription, value);
            return subscription;
        }

        public IDisposable Subscribe(IObservableInternal<T> observable, IObserver<T> observer)
        {
            var subscription = new ObservableSubscription<T>(observer);

            Subscribe(observable, subscription);
            return subscription;
        }

        public void Subscribe(IObservableInternal<T> observable, ObservableSubscription<T> subscription, T value)
        {
            subscription.OnNext(value);
            subscription.Observable = observable;
            subscription.Next = _head;
            _head = subscription;
        }

        public void Subscribe(IObservableInternal<T> observable, ObservableSubscription<T> subscription)
        {
            subscription.Observable = observable;
            subscription.Next = _head;
            _head = subscription;
        }

        public void Unsubscribe(ObservableSubscription<T> subscription)
        {
            ref var current = ref _head;
            while (current is not null)
            {
                if (current == subscription)
                {
                    current = subscription.Next;
                    subscription.Observable = null;
                    subscription.Next = null;
                    return;
                }

                current = ref current.Next;
            }
        }

        public void OnNext(T value)
        {
            var current = _head;
            while (current is not null)
            {
                current.OnNext(value);
                current = current.Next;
            }
        }

        public void OnError(Exception error)
        {
            var current = _head;
            while (current is not null)
            {
                current.OnError(error);
                current = current.Next;
            }
        }

        public void OnCompleted()
        {
            while (_head is { } head)
            {
                _head = head.Next;

                head.Next = null;
                head.OnCompleted();
            }
        }
    }

    public abstract class Observable<T> : IObservableInternal<T>
    {
        private ObservableSubscriptions<T> _subscriptions;

        public IDisposable Subscribe(IObserver<T> observer) =>
            _subscriptions.Subscribe(this, observer);

        void IObservableInternal<T>.Subscribe(ObservableSubscription<T> subscription) =>
            _subscriptions.Subscribe(this, subscription);

        void IObservableInternal<T>.Unsubscribe(ObservableSubscription<T> subscription) =>
            _subscriptions.Unsubscribe(subscription);

        protected void OnNext(T value) => _subscriptions.OnNext(value);
        protected void OnError(Exception error) => _subscriptions.OnError(error);
        protected void OnCompleted() => _subscriptions.OnCompleted();
    }
}