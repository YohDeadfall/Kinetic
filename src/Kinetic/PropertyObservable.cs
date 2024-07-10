using System;

namespace Kinetic;

internal abstract class PropertyObservable
{
    internal readonly ObservableObject Owner;
    internal readonly PropertyObservable? Next;
    internal readonly IntPtr Offset;

    internal uint Version;

    protected PropertyObservable(ObservableObject owner, IntPtr offset, PropertyObservable? next) =>
        (Owner, Offset, Next) = (owner, offset, next);

    public abstract void Changed();
}

internal sealed class PropertyObservable<T> : PropertyObservable, IObservableInternal<T>
{
    private ObservableSubscriptions<T> _subscriptions;

    public PropertyObservable(ObservableObject owner, IntPtr offset, PropertyObservable? next) :
        base(owner, offset, next)
    { }

    public override void Changed() =>
        Changed(Owner.Get<T>(Offset));

    public void Changed(T value) =>
        _subscriptions.OnNext(value);

    public IDisposable Subscribe(IObserver<T> observer)
    {
        observer.OnNext(Owner.Get<T>(Offset));
        return _subscriptions.Subscribe(this, observer);
    }

    public void Subscribe(ObservableSubscription<T> subscription) =>
        _subscriptions.Subscribe(this, subscription);

    public void Unsubscribe(ObservableSubscription<T> subscription) =>
        _subscriptions.Unsubscribe(subscription);
}