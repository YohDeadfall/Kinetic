using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Kinetic;

[DebuggerDisplay("Count = {Count}")]
public sealed class ObservableList<T> : ObservableObject, IList<T>, IReadOnlyList<T>
{
    private const int DefaultCapacity = 4;

    private T[] _items;
    private int _count;
    private int _version;

    public ObservableList() =>
        _items = Array.Empty<T>();

    public ObservableList(int capacity) =>
        _items = capacity < 0
        ? throw new ArgumentOutOfRangeException(nameof(capacity))
        : capacity > 0 ? new T[capacity] : Array.Empty<T>();

    public IObservable<ListChange<T>> Changed => EnsureChangeObservable();

    public ReadOnlyProperty<int> Count => Property(ref _count);

    int ICollection<T>.Count => _count;

    bool ICollection<T>.IsReadOnly => false;

    int IReadOnlyCollection<T>.Count => _count;

    public T this[int index]
    {
        get
        {
            if ((uint) index >= (uint) _count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return _items[index];
        }
        set
        {
            if ((uint) index >= (uint) _count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            var previous = _items[index];

            _items[index] = value;
            _version += 1;

            if (NotificationsEnabled)
            {
                GetChangeObservable()?.Replaced(index, previous, value);
            }
        }
    }

    public void Add(T item)
    {
        var index = _count;

        if ((uint) index == (uint) _items.Length)
        {
            Grow(index + 1);
        }

        _count = index + 1;
        _items[index] = item;
        _version += 1;

        if (NotificationsEnabled)
        {
            GetCountObservable()?.Changed(index);
            GetChangeObservable()?.Inserted(index, item);
        }
    }

    public void Clear()
    {
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            Array.Clear(_items, 0, _count);
        }

        _count = 0;
        _version += 1;

        if (NotificationsEnabled)
        {
            GetCountObservable()?.Changed(0);
            GetChangeObservable()?.Reset();
        }
    }

    public bool Contains(T item) =>
        _count != 0 && IndexOf(item) >= -1;

    public void CopyTo(T[] array)
        => CopyTo(array, 0);

    public void CopyTo(T[] array, int arrayIndex) =>
        Array.Copy(_items, 0, array, arrayIndex, _count);

    private void Grow(int capacity)
    {
        Debug.Assert(_items.Length < capacity);

        var newCapacity = _items.Length == 0
            ? DefaultCapacity
            : 2 * _items.Length;

        if (newCapacity > 0)
        {
            var newItems = new T[newCapacity];
            if (_count > 0)
            {
                Array.Copy(_items, newItems, _count);
            }

            _items = newItems;
        }
        else
        {
            _items = Array.Empty<T>();
        }
    }

    public Enumerator GetEnumerator() =>
        new Enumerator(this);

    IEnumerator<T> IEnumerable<T>.GetEnumerator() =>
        GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() =>
        GetEnumerator();

    public int IndexOf(T item) =>
        Array.IndexOf(_items, item, 0, _count);

    public int IndexOf(T item, int index) =>
        index > _count ? throw new ArgumentOutOfRangeException(nameof(index)) :
        Array.IndexOf(_items, item, index, _count - index);

    public int IndexOf(T item, int index, int count) =>
        index > _count ? throw new ArgumentOutOfRangeException(nameof(index)) :
        index > _count - count || count < 0 ? throw new ArgumentOutOfRangeException(nameof(count)) :
        Array.IndexOf(_items, item, index, count);

    public void Insert(int index, T item)
    {
        if ((uint) index > (uint) _count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        if (_count == _items.Length)
        {
            Grow(_count + 1);
        }

        if (index < _count)
        {
            Array.Copy(
                sourceArray: _items,
                sourceIndex: index,
                destinationArray: _items,
                destinationIndex: index + 1,
                length: _count - index);
        }

        _items[index] = item;
        _count += 1;
        _version += 1;

        if (NotificationsEnabled)
        {
            GetCountObservable()?.Changed(_count);
            GetChangeObservable()?.Removed(index, item);
        }
    }

    public bool Remove(T item)
    {
        var index = IndexOf(item);
        if (index != -1)
        {
            RemoveAt(index);

            return true;
        }

        return false;
    }

    public void RemoveAt(int index)
    {
        var item = (uint) index >= (uint) _count
            ? throw new ArgumentOutOfRangeException(nameof(index))
            : _items[index];

        _count -= 1;
        _version += 1;

        if (index < _count)
        {
            Array.Copy(
                sourceArray: _items,
                sourceIndex: index + 1,
                destinationArray: _items,
                destinationIndex: index,
                length: _count - index);
        }

        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
        {
            _items[_count] = default!;
        }

        if (NotificationsEnabled)
        {
            GetCountObservable()?.Changed(_count);
            GetChangeObservable()?.Removed(index, item);
        }
    }

    public void Move(int oldIndex, int newIndex)
    {
        if ((uint) oldIndex >= (uint) _count)
        {
            throw new ArgumentOutOfRangeException(nameof(oldIndex));
        }

        if ((uint) newIndex >= (uint) _count)
        {
            throw new ArgumentOutOfRangeException(nameof(newIndex));
        }

        var item = _items[oldIndex];

        Array.Copy(
            sourceArray: _items,
            sourceIndex: oldIndex + 1,
            destinationArray: _items,
            destinationIndex: oldIndex,
            length: _count - oldIndex);

        Array.Copy(
            sourceArray: _items,
            sourceIndex: newIndex,
            destinationArray: _items,
            destinationIndex: newIndex + 1,
            length: _count - newIndex);

        _items[newIndex] = item;
        _version += 1;

        if (NotificationsEnabled)
        {
            GetCountObservable()?.Changed(_count);
            GetChangeObservable()?.Moved(oldIndex, newIndex, item);
        }
    }

    public IDisposable Subscribe(IObserver<ListChange<T>> observer) =>
        EnsureChangeObservable().Subscribe(observer);

    private ListChangeObservable<T> EnsureChangeObservable() =>
        Unsafe.As<ListChangeObservable<T>>(EnsureObservable(GetOffsetOf(ref _items), static (self, offset, next) => new ListChangeObservable<T>(self, offset, next)));

    private ListChangeObservable<T>? GetChangeObservable() =>
        Unsafe.As<ListChangeObservable<T>?>(GetObservable(GetOffsetOf(ref _items)));

    private PropertyObservable<int>? GetCountObservable() =>
        Unsafe.As<PropertyObservable<int>?>(GetObservable(GetOffsetOf(ref _count)));

    public struct Enumerator : IEnumerator<T>
    {
        private readonly ObservableList<T> _items;
        private int _index;
        private readonly int _version;
        private T? _current;

        internal Enumerator(ObservableList<T> items)
        {
            _items = items;
            _index = 0;
            _version = items._version;
            _current = default;
        }

        public void Dispose()
        {
        }

        public bool MoveNext()
        {
            var items = _items;

            if (_version == items._version && ((uint) _index < (uint) items._count))
            {
                _current = items._items[_index];
                _index++;

                return true;
            }

            return MoveNextRare();
        }

        private bool MoveNextRare()
        {
            if (_version != _items._version)
            {
                throw new InvalidOperationException();
            }

            _index = _items._count + 1;
            _current = default;
            return false;
        }

        public T Current => _current!;

        object? IEnumerator.Current =>
            _index == 0 || _index == _items._count + 1
                ? throw new InvalidOperationException()
                : Current;

        void IEnumerator.Reset()
        {
            if (_version != _items._version)
            {
                throw new InvalidOperationException();
            }

            _index = 0;
            _current = default;
        }
    }
}

public static class ListChange
{
    public static ListChange<T> Insert<T>(int index, T item) =>
        new(newItem: item, newIndex: index);

    public static ListChange<T> Remove<T>(int index, T item) =>
        new(oldItem: item, oldIndex: index);

    public static ListChange<T> Replace<T>(int index, T oldItem, T newItem) =>
        new(oldItem, newItem, index, index);

    public static ListChange<T> Move<T>(int oldIndex, int newIndex, T item) =>
        new(item, item, oldIndex, newIndex);

    public static ListChange<T> Reset<T>() =>
        default;
}

public enum ListChangeAction
{
    Reset = 0,
    Insert = 1,
    Remove = 2,
    Replace = 3,
    Move = 4,
}

public readonly struct ListChange<T> : IEquatable<ListChange<T>>
{
    private readonly int _oldIndex;
    private readonly int _newIndex;
    private readonly T _oldItem;
    private readonly T _newItem;

    internal ListChange(
        T oldItem = default!,
        T newItem = default!,
        int oldIndex = -1,
        int newIndex = -1)
    {
        _oldItem = oldItem;
        _newItem = newItem;
        _oldIndex = ~oldIndex;
        _newIndex = ~newIndex;
    }

    public ListChangeAction Action
    {
        get
        {
            var action = (ListChangeAction) (
                ((_oldIndex & 0x80000000) >> 30) |
                ((_newIndex & 0x80000000) >> 31));

            return ListChangeAction.Replace == action && _oldIndex != _newIndex
                ? ListChangeAction.Move : action;
        }
    }

    public T OldItem => _oldIndex < 0 ? _oldItem : throw new InvalidOperationException();
    public T NewItem => _newIndex < 0 ? _newItem : throw new InvalidOperationException();

    public int OldIndex => _oldIndex < 0 ? ~_oldIndex : throw new InvalidOperationException();
    public int NewIndex => _newIndex < 0 ? ~_newIndex : throw new InvalidOperationException();

    public override int GetHashCode() => HashCode.Combine(_oldIndex, _newIndex, _oldItem, _newItem);

    public bool Equals(ListChange<T> other) =>
        _oldIndex == other._oldIndex &&
        _newIndex == other._newIndex &&
        EqualityComparer<T>.Default.Equals(_oldItem, other._oldItem) &&
        EqualityComparer<T>.Default.Equals(_newItem, other._newItem);
}

internal sealed class ListChangeObservable<T> : PropertyObservable, IObservableInternal<ListChange<T>>
{
    private ObservableSubscriptions<ListChange<T>> _subscriptions;

    public ListChangeObservable(ObservableObject owner, IntPtr offset, PropertyObservable? next)
        : base(owner, offset, next) { }

    public override void Changed() =>
        Reset();

    public void Inserted(int index, T item) =>
        _subscriptions.OnNext(ListChange.Insert(index, item));

    public void Removed(int index, T item) =>
        _subscriptions.OnNext(ListChange.Remove(index, item));

    public void Replaced(int index, T oldItem, T newItem) =>
        _subscriptions.OnNext(ListChange.Replace(index, oldItem, newItem));

    public void Moved(int oldIndex, int newIndex, T item) =>
        _subscriptions.OnNext(ListChange.Move(oldIndex, newIndex, item));

    public void Reset() =>
        _subscriptions.OnNext(ListChange.Reset<T>());

    public IDisposable Subscribe(IObserver<ListChange<T>> observer)
    {
        observer.OnNext(ListChange.Reset<T>());
        return _subscriptions.Subscribe(this, observer);
    }

    public void Subscribe(ObservableSubscription<ListChange<T>> subscription) =>
        _subscriptions.Subscribe(this, subscription);

    public void Unsubscribe(ObservableSubscription<ListChange<T>> subscription) =>
        _subscriptions.Unsubscribe(subscription);
}