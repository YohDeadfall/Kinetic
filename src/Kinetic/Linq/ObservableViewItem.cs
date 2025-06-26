using System;
using System.Diagnostics.CodeAnalysis;

namespace Kinetic.Linq;

internal sealed class ObservableViewItem<T> : IObserver<T>, IDisposable
{
    private const int PresentMask = 1 << 31;

    private readonly IObservableItemStateMachine<T> _stateMachine;
    private readonly IDisposable _subscription;
    private int _index;

    [AllowNull]
    public T Value { get; set; }

    public int Index
    {
        get => _index & ~PresentMask;
        set => _index = (_index & PresentMask) | value;
    }

    public bool Present
    {
        get => (_index & PresentMask) != 0;
        set => _index = value
            ? _index | PresentMask
            : _index & ~PresentMask;
    }

    public bool Initialized => _subscription is { };

    public ObservableViewItem(int index, IObservable<T> source, IObservableItemStateMachine<T> stateMachine)
    {
        _index = index;
        _stateMachine = stateMachine;
        _subscription = source.Subscribe(this);
    }

    public void Dispose() =>
        _subscription.Dispose();

    public void OnCompleted() =>
        _stateMachine.OnItemCompleted(this, null);

    public void OnError(Exception error) =>
        _stateMachine.OnItemCompleted(this, error);

    public void OnNext(T value) =>
        _stateMachine.OnItemUpdated(this);
}