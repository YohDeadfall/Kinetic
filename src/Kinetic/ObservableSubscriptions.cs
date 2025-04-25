using System;
using System.Collections.Generic;
using System.Threading;

namespace Kinetic;

internal struct ObservableSubscriptions<T>
{
    internal ObservableSubscription<T>? Head;

    public IDisposable Subscribe(IObserver<T> observer, IObservableInternal<T> observable)
    {
        var subscription = new ObservableSubscription<T>(observer);

        Subscribe(subscription, observable);
        return subscription;
    }

    public void Subscribe(ObservableSubscription<T> subscription, IObservableInternal<T> observable)
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