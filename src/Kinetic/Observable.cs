using System;
using System.Collections.Generic;
using System.Threading;

namespace Kinetic;

internal interface IObservableInternal<T> : IObservable<T>
{
    void Subscribe(ObservableSubscription<T> subscription);
    void Unsubscribe(ObservableSubscription<T> subscription);
}

internal sealed class ObservableSubscription<T> : IDisposable
{
    internal readonly IObserver<T> Observer;
    internal IObservableInternal<T>? Observable;
    internal ObservableSubscription<T>? Next;

    public ObservableSubscription(IObserver<T> observer) =>
        Observer = observer;

    public void Dispose() =>
        Observable?.Unsubscribe(this);

    public void OnNext(T value) =>
        Observer.OnNext(value);

    public void OnError(Exception error) =>
        Observer.OnError(error);

    public void OnCompleted() =>
        Observer.OnCompleted();
}

internal struct ObservableSubscriptions<T>
{
    internal ObservableSubscription<T>? Head;

    public IDisposable Subscribe(IObservableInternal<T> observable, IObserver<T> observer)
    {
        var subscription = new ObservableSubscription<T>(observer);

        Subscribe(observable, subscription);
        return subscription;
    }

    public void Subscribe(IObservableInternal<T> observable, ObservableSubscription<T> subscription)
    {
        subscription.Observable = observable;
        do
        {
            subscription.Next = Head;
        }
        while (Interlocked.CompareExchange(ref Head, subscription, subscription.Next) != subscription.Next);
    }

    public void Unsubscribe(ObservableSubscription<T> subscription)
    {
        ref var current = ref Head;
        while (current is not null)
        {
            if (current == subscription)
            {
                Interlocked.CompareExchange(ref current, subscription.Next, subscription);

                subscription.Observable = null;
                subscription.Next = null;

                return;
            }

            current = ref current.Next;
        }
    }

    public void OnNext(T value)
    {
        var current = Head;
        while (current is not null)
        {
            current.OnNext(value);
            current = current.Next;
        }
    }

    public void OnError(Exception error)
    {
        while (Head is { } head)
        {
            Head = head.Next;

            head.Next = null;
            head.OnError(error);
        }
    }

    public void OnCompleted()
    {
        while (Head is { } head)
        {
            Head = head.Next;

            head.Next = null;
            head.OnCompleted();
        }
    }

    public int GetObserversCountForDebugger()
    {
        var observers = 0;
        var current = Head;
        while (current is { })
        {
            observers += 1;
            current = current.Next;
        }

        return observers;
    }

    public IObserver<T>[] GetObserversForDebugger()
    {
        var observers = new List<IObserver<T>>();
        var current = Head;
        while (current is { })
        {
            observers.Add(current.Observer);
            current = current.Next;
        }

        return observers.ToArray();
    }
}