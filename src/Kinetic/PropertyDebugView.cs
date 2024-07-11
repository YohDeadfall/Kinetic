using System;

namespace Kinetic;

internal sealed class PropertyDebugView<T>
{
    private readonly Property<T> _property;

    public PropertyDebugView(Property<T> property) =>
        _property = property;

    public IObservable<T> Observers =>
        _property.Changed;

    public T Value
    {
        get => _property.Get();
        set => _property.Set(value);
    }
}