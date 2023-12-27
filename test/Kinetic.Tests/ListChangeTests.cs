using System;
using Xunit;

namespace Kinetic.Collections.Tests;

public class ListChangeTests
{
    [Theory]
    [InlineData(0, "one")]
    [InlineData(1, "two")]
    public void Insert(int index, string item)
    {
        var change = ListChange.Insert(index, item);

        Assert.Equal(ListChangeAction.Insert, change.Action);
        Assert.Equal(item, change.NewItem);
        Assert.Equal(index, change.NewIndex);
        Assert.Throws<InvalidOperationException>(() => change.OldIndex);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void Remove(int index)
    {
        var change = ListChange.Remove<string>(index);

        Assert.Equal(ListChangeAction.Remove, change.Action);
        Assert.Throws<InvalidOperationException>(() => change.NewItem);
        Assert.Throws<InvalidOperationException>(() => change.NewIndex);
        Assert.Equal(index, change.OldIndex);
    }

    [Fact]
    public void RemoveAll()
    {
        var change = ListChange.RemoveAll<string>();

        Assert.Equal(default, change);
        Assert.Equal(ListChangeAction.RemoveAll, change.Action);
        Assert.Throws<InvalidOperationException>(() => change.NewItem);
        Assert.Throws<InvalidOperationException>(() => change.NewIndex);
        Assert.Throws<InvalidOperationException>(() => change.OldIndex);
    }

    [Theory]
    [InlineData(0, "one")]
    [InlineData(1, "two")]
    public void Replace(int index, string item)
    {
        var change = ListChange.Replace(index, item);

        Assert.Equal(ListChangeAction.Replace, change.Action);
        Assert.Equal(item, change.NewItem);
        Assert.Equal(index, change.NewIndex);
        Assert.Equal(index, change.OldIndex);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 2)]
    public void Move(int oldIndex, int newIndex)
    {
        var change = ListChange.Move<string>(oldIndex, newIndex);

        Assert.Equal(ListChangeAction.Move, change.Action);
        Assert.Throws<InvalidOperationException>(() => change.NewItem);
        Assert.Equal(newIndex, change.NewIndex);
        Assert.Equal(oldIndex, change.OldIndex);
    }
}