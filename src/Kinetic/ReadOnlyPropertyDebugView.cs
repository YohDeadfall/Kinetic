using System;
using System.Collections.Generic;

namespace Kinetic;

internal sealed class ReadOnlyPropertyDebugView<T>
{
    public ReadOnlyPropertyDebugView(Property<T> property)
    {
        var subscribers = new List<IObserver<T>>();
        var subscription = property.Owner
            .GetObservableFor<T>(property.Offset)?
            .Subscriptions.Head;
        while (subscription is { })
        {
            subscribers.Add(subscription.Observer);
            subscription = subscription.Next;
        }

        Subscribers = subscribers;
        Value = property.Get();
    }

    public T Value { get; }

    public IReadOnlyList<IObserver<T>> Subscribers { get; }
}