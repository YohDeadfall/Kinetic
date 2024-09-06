using System;
using System.Diagnostics;

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

[DebuggerDisplay("Observers = {Subscriptions.GetObserversCountForDebugger()}")]
[DebuggerTypeProxy(typeof(PropertyObservableDebugView<>))]
internal sealed class PropertyObservable<T> : PropertyObservable, IObservableInternal<T>, IObserver<T>
{
    internal ObservableSubscriptions<T> Subscriptions;

    public PropertyObservable(ObservableObject owner, IntPtr offset, PropertyObservable? next) :
        base(owner, offset, next)
    { }

    public override void Changed() =>
        Changed(Owner.Get<T>(Offset));

    public void Changed(T value) =>
        Subscriptions.OnNext(value);

    public IDisposable Subscribe(IObserver<T> observer)
    {
        observer.OnNext(Owner.Get<T>(Offset));
        return Subscriptions.Subscribe(this, observer);
    }

    public void Subscribe(ObservableSubscription<T> subscription) =>
        Subscriptions.Subscribe(this, subscription);

    public void Unsubscribe(ObservableSubscription<T> subscription) =>
        Subscriptions.Unsubscribe(subscription);

    public void OnCompleted()
    { }

    public void OnError(Exception error)
    { }

    public void OnNext(T value) =>
        Owner.Set(Offset, value);
}