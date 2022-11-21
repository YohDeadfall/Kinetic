using System.Collections.Generic;
using System.Linq;
using Kinetic.Linq;
using Xunit;

namespace Kinetic.Tests;

public class ObservableListTests
{
    [Fact]
    public void Add()
    {
        var actual = new ObservableList<int>();
        var actualChanges = new List<ListChange<int>>();

        var expected = new[] { 1, 3, 5, 2, 4 };
        var expectedChanges = expected
            .Take(4)
            .Select((e, i) => ListChange.Insert(i, e))
            .Prepend(ListChange.Reset<int>())
            .ToArray();

        using (actual.Changed.Subscribe(change => actualChanges.Add(change)))
        {
            foreach (var element in expected.Take(4))
            {
                actual.Add(element);
            }
        }

        actual.Add(expected.Last());

        Assert.Equal(expected, actual);
        Assert.Equal(expectedChanges, actualChanges);
    }

    [Fact]
    public void Clear()
    {
        var actual = new ObservableList<int>();
        var actualChanges = new List<ListChange<int>>();

        foreach (var element in new[] { 1, 3, 5, 2, 4 })
        {
            actual.Add(element);
        }

        using (actual.Changed.Subscribe(change => actualChanges.Add(change)))
        {
            actual.Clear();
        }

        Assert.Equal(new int[] { }, actual);
        Assert.Equal(new[] { ListChange.Reset<int>(), ListChange.Reset<int>() }, actualChanges);
    }

    [Fact]
    public void Replace()
    {
        var actual = new ObservableList<int>();
        var actualChanges = new List<ListChange<int>>();

        foreach (var element in new[] { 1, 3, 5, 2, 4 })
        {
            actual.Add(element);
        }

        using (actual.Changed.Subscribe(change => actualChanges.Add(change)))
        {
            actual[2] = 0;
        }

        actual[0] = 7;
        actual[4] = 8;

        Assert.Equal(new int[] { 7, 3, 0, 2, 8 }, actual);
        Assert.Equal(new[] { ListChange.Reset<int>(), ListChange.Replace(2, 5, 0) }, actualChanges);
    }

    [Fact]
    public void Remove()
    {
        var actual = new ObservableList<int>();
        var actualChanges = new List<ListChange<int>>();

        foreach (var element in new[] { 1, 3, 5, 2, 4 })
        {
            actual.Add(element);
        }

        using (actual.Changed.Subscribe(change => actualChanges.Add(change)))
        {
            actual.Remove(0);
            actual.Remove(3);
        }

        actual.Remove(2);
        actual.Remove(3);

        Assert.Equal(new int[] { 1, 5, 4 }, actual);
        Assert.Equal(new[] { ListChange.Reset<int>(), ListChange.Remove(1, 3) }, actualChanges);
    }

    [Fact]
    public void RemoveAt()
    {
        var actual = new ObservableList<int>();
        var actualChanges = new List<ListChange<int>>();

        foreach (var element in new[] { 1, 3, 5, 2, 4 })
        {
            actual.Add(element);
        }

        using (actual.Changed.Subscribe(change => actualChanges.Add(change)))
        {
            actual.RemoveAt(3);
        }

        actual.RemoveAt(0);

        Assert.Equal(new int[] { 3, 5, 4 }, actual);
        Assert.Equal(new[] { ListChange.Reset<int>(), ListChange.Remove(3, 2) }, actualChanges);
    }
}