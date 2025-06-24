using System;
using System.Diagnostics.CodeAnalysis;

namespace Kinetic.Linq;

internal sealed class ObservableViewItem<T> : IDisposable
{
    private const int PresentMask = 1 << 31;

    private IDisposable? _subscription;
    private int _index;

    [AllowNull]
    public T Item { get; set; }

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

    public ObservableViewItem(int index) =>
        Index = index;

    public void Dispose() =>
        _subscription?.Dispose();

    public void Initialize(IDisposable subscription) =>
        _subscription = subscription;
}