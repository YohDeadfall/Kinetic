using System.Collections.Generic;

namespace Kinetic;

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