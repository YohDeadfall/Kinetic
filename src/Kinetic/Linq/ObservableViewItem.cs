using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Kinetic.Linq;

internal abstract class ObservableViewItem : IDisposable
{
    private const int PresentMask = 1 << 31;
    private int _index;

    public int Index
    {
        get => _index & ~PresentMask;
        private set => _index = (_index & PresentMask) | value;
    }

    public bool Present
    {
        get => (_index & PresentMask) != 0;
        set => _index = value
            ? _index | PresentMask
            : _index & ~PresentMask;
    }

    public ObservableViewItem(int index) =>
        _index = index;

    public abstract void Dispose();

    public static int GetAdjustedIndex<TItem>(List<TItem> items, int index)
        where TItem : ObservableViewItem
    {
        var count = 0;
        var span = CollectionsMarshal.AsSpan(items);
        while (true)
        {
            if (--index < 0)
                break;

            if (span[index].Present)
                count += 1;
        }
        return count;
    }

    public static void Insert<TItem>(List<TItem> items, int index, TItem item)
        where TItem : ObservableViewItem
    {
        items.Insert(index, item);
        foreach (var other in CollectionsMarshal.AsSpan(items).Slice(index))
            other.Index += 1;
    }

    public static TItem Replace<TItem>(List<TItem> items, int index, TItem item)
        where TItem : ObservableViewItem
    {
        var oldItem = items[index];

        oldItem.Dispose();
        items[index] = item;

        return oldItem;
    }

    public static TItem Remove<TItem>(List<TItem> items, int index)
        where TItem : ObservableViewItem
    {
        var item = items[index];

        item.Dispose();
        items.RemoveAt(index);
        foreach (var other in CollectionsMarshal.AsSpan(items).Slice(index))
            other.Index += 1;

        return item;
    }

    public static TItem Move<TItem>(List<TItem> items, int index, int newIndex)
        where TItem : ObservableViewItem
    {
        var item = items[index];

        items.RemoveAt(index);
        items.Insert(newIndex, item);
        item.Index = newIndex;

        var (starting, count, change) = newIndex > index
            ? (index, newIndex - index, -1)
            : (newIndex + 1, index - newIndex, 1);

        foreach (var other in CollectionsMarshal.AsSpan(items).Slice(starting, count))
            other.Index += change;

        return item;
    }
}