using System;
using Xunit;

namespace Kinetic.Collections.Tests;

public class ListChangeTests
{
    [Theory]
    [InlineData(0, "zero")]
    [InlineData(1, "one")]
    public void Insert(int index, string item)
    {
        var change = ListChange.Insert(index, item);

        Assert.Equal(ListChangeAction.Insert, change.Action);
        Assert.Equal(item, change.NewItem);
        Assert.Equal(index, change.NewIndex);
        Assert.Throws<InvalidOperationException>(() => change.OldItem);
        Assert.Throws<InvalidOperationException>(() => change.OldIndex);
    }

    [Theory]
    [InlineData(0, "zero")]
    [InlineData(1, "one")]
    public void Remove(int index, string item)
    {
        var change = ListChange.Remove(index, item);

        Assert.Equal(ListChangeAction.Remove, change.Action);
        Assert.Throws<InvalidOperationException>(() => change.NewItem);
        Assert.Throws<InvalidOperationException>(() => change.NewIndex);
        Assert.Equal(item, change.OldItem);
        Assert.Equal(index, change.OldIndex);
    }

    [Theory]
    [InlineData(0, "zero", "one")]
    [InlineData(1, "one", "two")]
    public void Replace(int index, string oldItem, string newItem)
    {
        var change = ListChange.Replace(index, oldItem, newItem);

        Assert.Equal(ListChangeAction.Replace, change.Action);
        Assert.Equal(newItem, change.NewItem);
        Assert.Equal(index, change.NewIndex);
        Assert.Equal(oldItem, change.OldItem);
        Assert.Equal(index, change.OldIndex);
    }

    [Theory]
    [InlineData(0, 1, "zero")]
    [InlineData(1, 2, "one")]
    public void Move(int oldIndex, int newIndex, string item)
    {
        var change = ListChange.Move(oldIndex, newIndex, item);

        Assert.Equal(ListChangeAction.Move, change.Action);
        Assert.Equal(item, change.NewItem);
        Assert.Equal(newIndex, change.NewIndex);
        Assert.Equal(item, change.OldItem);
        Assert.Equal(oldIndex, change.OldIndex);
    }

    [Fact]
    public void Reset()
    {
        var change = ListChange.Reset<string>();

        Assert.Equal(default, change);
        Assert.Equal(ListChangeAction.Reset, change.Action);
        Assert.Throws<InvalidOperationException>(() => change.NewItem);
        Assert.Throws<InvalidOperationException>(() => change.NewIndex);
        Assert.Throws<InvalidOperationException>(() => change.OldItem);
        Assert.Throws<InvalidOperationException>(() => change.OldIndex);
    }
}