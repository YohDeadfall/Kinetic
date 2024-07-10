using System;
using System.Collections.Generic;

namespace Kinetic;

internal sealed class PropertyDebugView<T>
{
    private readonly Property<T> _property;

    public PropertyDebugView(Property<T> property)
    {
        _property = property;

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
    }

    public T Value
    {
        get => _property.Get();
        set => _property.Set(value);
    }

    public IReadOnlyList<IObserver<T>> Subscribers { get; }
}