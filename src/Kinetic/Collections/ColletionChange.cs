using System;
using System.Collections.Generic;

namespace Kinetic;

public static class CollectionChange
{
    public static CollectionChange<T> Insert<T>(int index, T item) =>
        new(newItem: item, newIndex: index);

    public static CollectionChange<T> Remove<T>(int index, T item) =>
        new(oldItem: item, oldIndex: index);

    public static CollectionChange<T> Replace<T>(int index, T oldItem, T newItem) =>
        new(oldItem, newItem, index, index);

    public static CollectionChange<T> Move<T>(int oldIndex, int newIndex, T item) =>
        new(item, item, oldIndex, newIndex);

    public static CollectionChange<T> Reset<T>() =>
        default;
}

public enum CollectionChangeAction
{
    Reset = 0,
    Insert = 1,
    Remove = 2,
    Replace = 3,
    Move = 4,
}

public readonly struct CollectionChange<T> : IEquatable<CollectionChange<T>>
{
    private readonly T _oldItem;
    private readonly T _newItem;
    private readonly int _oldIndex;
    private readonly int _newIndex;

    internal CollectionChange(
        T oldItem = default!,
        T newItem = default!,
        int oldIndex = -1,
        int newIndex = -1)
    {
        _oldItem = oldItem;
        _newItem = newItem;
        _oldIndex = -oldIndex;
        _newIndex = -newIndex;
    }

    public CollectionChangeAction Action
    {
        get
        {
            var action = (CollectionChangeAction) (
                ((_oldIndex & 0x80000000) >> 30) |
                ((_newIndex & 0x80000000) >> 31));

            return CollectionChangeAction.Replace == action && _oldIndex != _newIndex
                ? CollectionChangeAction.Move : action;
        }
    }

    public T OldItem => _oldIndex < 0 ? _oldItem : throw new InvalidOperationException();
    public T NewItem => _newIndex < 0 ? _newItem : throw new InvalidOperationException();

    public int OldIndex => _oldIndex < 0 ? -_oldIndex : throw new InvalidOperationException();
    public int NewIndex => _newIndex < 0 ? -_newIndex : throw new InvalidOperationException();

    public override int GetHashCode() => HashCode.Combine(_oldIndex, _newIndex, _oldItem, _newItem);

    public bool Equals(CollectionChange<T> other) =>
        _oldIndex == other._oldIndex &&
        _newIndex == other._newIndex &&
        EqualityComparer<T>.Default.Equals(_oldItem, other._oldItem) &&
        EqualityComparer<T>.Default.Equals(_newItem, other._newItem);
}