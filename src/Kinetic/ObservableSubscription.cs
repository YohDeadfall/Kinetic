using System;

namespace Kinetic;

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
