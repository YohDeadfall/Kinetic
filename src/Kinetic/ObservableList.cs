using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Kinetic;

/// <summary>A read-only list with observable collection changes.<summary>
/// <seealso cref="ObservableList{T}"/>
[DebuggerDisplay("Count = {Count}")]
public abstract class ReadOnlyObservableList<T> : ObservableObject, IReadOnlyList<T>
{
    private const int DefaultCapacity = 4;

    private T[] _items;
    private int _count;
    private int _version;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReadOnlyObservableList{T}"/> class
    /// that is empty and has the default initial capacity.
    /// </summary>
    protected ReadOnlyObservableList() =>
        _items = Array.Empty<T>();

    /// <summary>
    /// Initializes a new instance of the <see cref="ReadOnlyObservableList{T}"/> class
    /// that is empty and has the specified initial capacity.
    /// </summary>
    protected ReadOnlyObservableList(int capacity) =>
        _items = capacity < 0
        ? throw new ArgumentOutOfRangeException(nameof(capacity))
        : capacity > 0 ? new T[capacity] : Array.Empty<T>();

    /// <summary>Gets an <see cref="IObservable{T}"/> notifying about collection changes of this list.</summary>
    /// <returns>An <see cref="IObservable{T}"/> notifying about collection changes of this list.</returns>
    public IObservable<ListChange<T>> Changed => EnsureChangeObservable();

    /// <summary>Gets the number of elements contained in the list.</summary>
    /// <returns>A <see cref="ReadOnlyProperty{Int32}"/> providing the number of elements contained in the list.</returns>
    public ReadOnlyProperty<int> Count => Property(ref _count);

    int IReadOnlyCollection<T>.Count => ItemCount;

    /// <summary>Gets the number of elements contained in the list.</summary>
    /// <returns>The number of elements contained in the list.</returns>
    /// <seealso cref="Count"/>
    protected int ItemCount => _count;

    /// <summary>Gets the element at the specified index.</summary>
    /// <param name="index">The zero-based index of the element to get.</param>
    /// <value>The element at the specified index.</value>
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
    }

    /// <summary>Adds an object to the end of the list.</summary>
    /// <param name="item">The object to be added to the end of the list.</param>
    protected void AddItem(T item)
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

    /// <summary>Removes all elements from the list.</summary>
    protected void ClearItems()
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
            GetChangeObservable()?.RemovedAll();
        }
    }

    /// <summary>Determines whether an element is in the list.</summary>
    /// <param name="item">The object to locate in the list.</param>
    /// <returns><see langword="true"/> if item is found in the list; otherwise, <see langword="false"/>.</returns>
    public bool Contains(T item) =>
        _count != 0 && IndexOf(item) >= -1;

    /// <summary>
    /// Copies the entire list to a compatible one-dimensional array,
    /// starting at the beginning of the target array.</summary>
    /// <param name="array">The one-dimensional array that is the destination of the elements copied from list.</param>
    public void CopyTo(T[] array) =>
        CopyTo(array, 0);

    /// <summary>
    /// Copies the entire list to a compatible one-dimensional array,
    /// starting at the specified index of the target array.
    /// </summary>
    /// <param name="array">The one-dimensional array that is the destination of the elements copied from list.</param>
    /// <param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param>
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

    /// <summary>Returns an enumerator that iterates through the list.</summary>
    /// <returns>A <see cref="ReadOnlyObservableList{T}.Enumerator"/> for the list.</returns>
    public Enumerator GetEnumerator() =>
        new Enumerator(this);

    IEnumerator<T> IEnumerable<T>.GetEnumerator() =>
        GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() =>
        GetEnumerator();

    /// <summary>
    /// Searches for the specified object and returns the zero-based index
    /// of the first occurrence within the entire list.
    /// </summary>
    /// <param name="item">The object to locate in the list.</param>
    /// <returns>
    /// The zero-based index of the first occurrence of <paramref name="item"/>
    /// within the entire list, if found; otherwise, -1.
    /// <returns>
    public int IndexOf(T item) =>
        Array.IndexOf(_items, item, 0, _count);

    /// <summary>
    /// Searches for the specified object and returns the zero-based index
    /// of the first occurrence within the range of elements in the list
    /// that extends from the specified index to the last element.
    /// </summary>
    /// <param name="item">The object to locate in the list.</param>
    /// <param name="index">The zero-based starting index of the search.</param>
    /// <returns>
    /// The zero-based index of the first occurrence of <paramref name="item"/>
    /// within the range of elements in the list that extends from <paramref name="index"/>
    /// to the last element, if found; otherwise, -1.
    /// </returns>
    public int IndexOf(T item, int index) =>
        index > _count ? throw new ArgumentOutOfRangeException(nameof(index)) :
        Array.IndexOf(_items, item, index, _count - index);

    /// <summary>
    /// Searches for the specified object and returns the zero-based index
    /// of the first occurrence within the range of elements in the list
    /// that starts at the specified index and contains the specified number of elements.
    /// </summary>
    /// <param name="item">The object to locate in the list.</param>
    /// <param name="index">The zero-based starting index of the search.</param>
    /// <returns>
    /// The zero-based index of the first occurrence of <paramref name="item"/>
    /// within the range of elements in the list that starts at <paramref name="index"/>
    /// and contains <paramref name="count"/> number of elements, if found; otherwise, -1.
    /// </returns>
    public int IndexOf(T item, int index, int count) =>
        index > _count ? throw new ArgumentOutOfRangeException(nameof(index)) :
        index > _count - count || count < 0 ? throw new ArgumentOutOfRangeException(nameof(count)) :
        Array.IndexOf(_items, item, index, count);

    /// <summary>Inserts an element into the list at the specified index.</summary>
    /// <param name="index">The zero-based index at which <paramref name="item"/> should be inserted.</param>
    /// <param name="item">The object to insert.</param>
    protected void InsertItem(int index, T item)
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
            GetChangeObservable()?.Inserted(index, item);
        }
    }

    /// <summary>Removes the first occurrence of a specific object from the list.</summary>
    /// <param name="item">The object to remove.</param>
    /// <returns>
    /// <see langword="true"/> if item is found and removed; otherwise, <see langword="false".
    /// </returns>
    protected bool RemoveItem(T item)
    {
        var index = IndexOf(item);
        if (index != -1)
        {
            RemoveItemAt(index);

            return true;
        }

        return false;
    }

    /// <summary>Removes the element at the specified index of the list.</summary>
    /// <param name="index">The zero-based index of the element to remove.</param>
    protected void RemoveItemAt(int index)
    {
        if ((uint) index >= (uint) _count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

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
            GetChangeObservable()?.Removed(index);
        }
    }

    /// <summary>Replaces the element at the specified index of the list by the provided one.</summary>
    /// <param name="index">The zero-based index of the element to replace.</param>
    /// <param name="item">The item to set at the specified index.</param>
    protected void ReplaceItem(int index, T item)
    {
        if ((uint) index >= (uint) _count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        _items[index] = item;
        _version += 1;

        if (NotificationsEnabled)
        {
            GetChangeObservable()?.Replaced(index, item);
        }
    }

    /// <summary>Moves the item at the specified index to a new location in the collection.</summary>
    /// <param name="oldIndex">The zero-based index specifying the location of the item to be moved.</param>
    /// <param name="newIndex">The zero-based index specifying the new location of the item.</param>
    protected void MoveItem(int oldIndex, int newIndex)
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
            length: _count - oldIndex - 1);

        Array.Copy(
            sourceArray: _items,
            sourceIndex: newIndex,
            destinationArray: _items,
            destinationIndex: newIndex + 1,
            length: _count - newIndex - 1);

        _items[newIndex] = item;
        _version += 1;

        if (NotificationsEnabled)
        {
            GetCountObservable()?.Changed(_count);
            GetChangeObservable()?.Moved(oldIndex, newIndex);
        }
    }

    private ItemsObservable EnsureChangeObservable() =>
        Unsafe.As<ItemsObservable>(EnsureObservable(GetOffsetOf(ref _items), static (self, offset, next) => new ItemsObservable(self, offset, next)));

    private ItemsObservable? GetChangeObservable() =>
        Unsafe.As<ItemsObservable?>(GetObservable(GetOffsetOf(ref _items)));

    private PropertyObservable<int>? GetCountObservable() =>
        Unsafe.As<PropertyObservable<int>?>(GetObservable(GetOffsetOf(ref _count)));

    /// <summary>Enumerates the elements of a list.</summary>
    public struct Enumerator : IEnumerator<T>
    {
        private readonly ReadOnlyObservableList<T> _items;
        private int _index;
        private readonly int _version;
        private T? _current;

        internal Enumerator(ReadOnlyObservableList<T> items)
        {
            _items = items;
            _index = 0;
            _version = items._version;
            _current = default;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
        }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
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

    private sealed class ItemsObservable : PropertyObservable, IObservableInternal<ListChange<T>>
    {
        private ObservableSubscriptions<ListChange<T>> _subscriptions;

        public ItemsObservable(ObservableObject owner, IntPtr offset, PropertyObservable? next)
            : base(owner, offset, next) { }

        public override void Changed()
        {
            _subscriptions.OnNext(ListChange.RemoveAll<T>());

            var items = Unsafe.As<ReadOnlyObservableList<T>>(Owner);

            for (int index = 0, count = items.Count; index < count; index += 1)
                _subscriptions.OnNext(ListChange.Insert(index, items[index]));
        }

        public void RemovedAll() =>
            _subscriptions.OnNext(ListChange.RemoveAll<T>());

        public void Removed(int index) =>
            _subscriptions.OnNext(ListChange.Remove<T>(index));

        public void Inserted(int index, T item) =>
            _subscriptions.OnNext(ListChange.Insert(index, item));

        public void Replaced(int index, T item) =>
            _subscriptions.OnNext(ListChange.Replace(index, item));

        public void Moved(int oldIndex, int newIndex) =>
            _subscriptions.OnNext(ListChange.Move<T>(oldIndex, newIndex));

        public IDisposable Subscribe(IObserver<ListChange<T>> observer)
        {
            observer.OnNext(ListChange.RemoveAll<T>());

            var items = Unsafe.As<ReadOnlyObservableList<T>>(Owner);

            for (int index = 0, count = items.Count; index < count; index += 1)
                observer.OnNext(ListChange.Insert(index, items[index]));

            return _subscriptions.Subscribe(this, observer);
        }

        public void Subscribe(ObservableSubscription<ListChange<T>> subscription) =>
            _subscriptions.Subscribe(this, subscription);

        public void Unsubscribe(ObservableSubscription<ListChange<T>> subscription) =>
            _subscriptions.Unsubscribe(subscription);
    }
}

/// <summary>
/// A list with observable collection changes.
/// </summary>
public sealed class ObservableList<T> : ReadOnlyObservableList<T>, IList<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ObservableList{T}"/> class
    /// that is empty and has the default initial capacity.
    /// </summary>
    public ObservableList() : base() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ObservableList{T}"/> class
    /// that is empty and has the specified initial capacity.
    /// </summary>
    public ObservableList(int capacity) : base(capacity) { }

    int ICollection<T>.Count => ItemCount;

    bool ICollection<T>.IsReadOnly => false;

    /// <summary>
    /// Gets or sets the element at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the element to get.</param>
    /// <value>The element at the specified index.</value>
    public new T this[int index]
    {
        get => base[index];
        set => ReplaceItem(index, value);
    }

    /// <summary>Adds an object to the end of the list.</summary>
    /// <param name="item">The object to be added to the end of the list.</param>
    public void Add(T item) =>
        AddItem(item);

    /// <summary>Removes all elements from the list.</summary>
    public void Clear() =>
        ClearItems();

    /// <summary>Inserts an element into the list at the specified index.</summary>
    /// <param name="index">The zero-based index at which <paramref name="item"/> should be inserted.</param>
    /// <param name="item">The object to insert.</param>
    public void Insert(int index, T item) =>
        InsertItem(index, item);

    /// <summary>Removes the first occurrence of a specific object from the list.</summary>
    /// <param name="item">The object to remove.</param>
    /// <returns>
    /// <see langword="true"/> if item is found and removed; otherwise, <see langword="false".
    /// </returns>
    public bool Remove(T item) =>
        RemoveItem(item);

    /// <summary>Removes the element at the specified index of the list.</summary>
    /// <param name="index">The zero-based index of the element to remove.</param>
    public void RemoveAt(int index) =>
        RemoveItemAt(index);

    /// <summary>Moves the item at the specified index to a new location in the collection.</summary>
    /// <param name="oldIndex">The zero-based index specifying the location of the item to be moved.</param>
    /// <param name="newIndex">The zero-based index specifying the new location of the item.</param>
    public void Move(int oldIndex, int newIndex) =>
        MoveItem(oldIndex, newIndex);
}