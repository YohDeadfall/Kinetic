using System;

namespace Kinetic.Subjects;

public abstract class Subject<T> : IObservableInternal<T>, IObserver<T>, IDisposable
{
    private protected ObservableSubscriptions<T> _subscriptions;
    private protected SubjectState _state;
    private protected Exception? _error;

    public IDisposable Subscribe(IObserver<T> observer) =>
        _subscriptions.Subscribe(this, observer);

    void IObservableInternal<T>.Subscribe(ObservableSubscription<T> subscription)
    {
        ThrowIfDisposed();

        if (_state == SubjectState.Completed)
        {
            if (_error is { } error)
            {
                subscription.OnError(error);
            }
            else
            {
                subscription.OnCompleted();
            }
        }

        _subscriptions.Subscribe(this, subscription);
    }

    void IObservableInternal<T>.Unsubscribe(ObservableSubscription<T> subscription) =>
        _subscriptions.Unsubscribe(subscription);

    private protected void ThrowIfDisposed()
    {
        if (_state == SubjectState.Disposed)
        {
            throw new ObjectDisposedException(GetType().Name);
        }
    }

    public virtual void OnNext(T value)
    {
        ThrowIfDisposed();

        if (_state == SubjectState.Alive)
        {
            _subscriptions.OnNext(value);
        }
    }

    public virtual void OnError(Exception error)
    {
        ThrowIfDisposed();

        if (_state == SubjectState.Alive)
        {
            _error = error;
            _state = SubjectState.Completed;
            _subscriptions.OnError(error);
        }
    }

    public virtual void OnCompleted()
    {
        ThrowIfDisposed();

        if (_state == SubjectState.Alive)
        {
            _state = SubjectState.Completed;
            _subscriptions.OnCompleted();
        }
    }

    public void Dispose()
    {
        if (_state != SubjectState.Disposed)
        {
            _error = null;
            _state = SubjectState.Disposed;
            _subscriptions.OnCompleted();
        }
    }
}

internal enum SubjectState
{
    Alive,
    Completed,
    Disposed
}