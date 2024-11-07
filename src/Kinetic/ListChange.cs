using System;
using System.Collections.Generic;

namespace Kinetic;

public enum ListChangeAction
{
    RemoveAll = 0,
    Remove = 1,
    Insert = 2,
    Replace = 3,
    Move = 4,
}

public static class ListChange
{
    public static ListChange<T> RemoveAll<T>() =>
        default;

    public static ListChange<T> Remove<T>(int index) =>
        new(oldIndex: index);

    public static ListChange<T> Insert<T>(int index, T item) =>
        new(newIndex: index, newItem: item);

    public static ListChange<T> Replace<T>(int index, T item) =>
        new(oldIndex: index, newIndex: index, item);

    public static ListChange<T> Move<T>(int oldIndex, int newIndex) =>
        new(oldIndex, newIndex);
}

public readonly struct ListChange<T> : IEquatable<ListChange<T>>
{
    private readonly int _oldIndex;
    private readonly int _newIndex;
    private readonly T _newItem;

    internal ListChange(
        int oldIndex = -1,
        int newIndex = -1,
        T newItem = default!)
    {
        _oldIndex = ~oldIndex;
        _newIndex = ~newIndex;
        _newItem = newItem;
    }

    public ListChangeAction Action
    {
        get
        {
            var action = (ListChangeAction) (
                ((_oldIndex & 0x80000000) >> 31) |
                ((_newIndex & 0x80000000) >> 30));

            return ListChangeAction.Replace == action && _oldIndex != _newIndex
                ? ListChangeAction.Move : action;
        }
    }

    public T NewItem => (_oldIndex < 0) == (_oldIndex == _newIndex) ? _newItem : throw new InvalidOperationException();

    public int OldIndex => _oldIndex < 0 ? ~_oldIndex : throw new InvalidOperationException();
    public int NewIndex => _newIndex < 0 ? ~_newIndex : throw new InvalidOperationException();

    public override int GetHashCode() => HashCode.Combine(_oldIndex, _newIndex, _newItem);

    public bool Equals(ListChange<T> other) =>
        _oldIndex == other._oldIndex &&
        _newIndex == other._newIndex &&
        EqualityComparer<T>.Default.Equals(_newItem, other._newItem);
}