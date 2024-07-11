using System;

namespace Kinetic;

internal sealed class ReadOnlyPropertyDebugView<T>
{
    private readonly ReadOnlyProperty<T> _property;

    public ReadOnlyPropertyDebugView(ReadOnlyProperty<T> property) =>
        _property = property;

    public IObservable<T> Observers =>
        _property.Changed;

    public T Value =>
        _property.Get();

}