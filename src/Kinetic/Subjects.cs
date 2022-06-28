using System;

namespace Kinetic.Subjects;

public class Subject<T> : IObservableInternal<T>, IObserver<T>, IDisposable
{
    private protected ObservableSubscriptions<T> _subscriptions;
    private protected bool _disposed;

    public IDisposable Subscribe(IObserver<T> observer) =>
        _subscriptions.Subscribe(this, observer);

    void IObservableInternal<T>.Subscribe(ObservableSubscription<T> subscription) =>
        _subscriptions.Subscribe(this, subscription);

    void IObservableInternal<T>.Unsubscribe(ObservableSubscription<T> subscription) =>
        _subscriptions.Unsubscribe(subscription);

    private protected void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(GetType().Name);
        }
    }

    public virtual void OnNext(T value)
    {
        ThrowIfDisposed();

        _subscriptions.OnNext(value);
    }

    public virtual void OnError(Exception error)
    {
        ThrowIfDisposed();

        _disposed = true;
        _subscriptions.OnError(error);
    }

    public virtual void OnCompleted()
    {
        ThrowIfDisposed();

        _disposed = true;
        _subscriptions.OnCompleted();
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            OnCompleted();
        }
    }
}