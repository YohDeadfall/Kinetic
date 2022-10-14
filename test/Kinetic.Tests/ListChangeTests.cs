using System;
using Xunit;

namespace Kinetic.Collections.Tests;

public class ListChangeTests
{
    [Fact]
    public void Insert()
    {
        var change = ListChange.Insert(1, "one");

        Assert.Equal(ListChangeAction.Insert, change.Action);
        Assert.Equal("one", change.NewItem);
        Assert.Equal(1, change.NewIndex);
        Assert.Throws<InvalidOperationException>(() => change.OldItem);
        Assert.Throws<InvalidOperationException>(() => change.OldIndex);
    }

    [Fact]
    public void Remove()
    {
        var change = ListChange.Remove(1, "one");

        Assert.Equal(ListChangeAction.Remove, change.Action);
        Assert.Throws<InvalidOperationException>(() => change.NewItem);
        Assert.Throws<InvalidOperationException>(() => change.NewIndex);
        Assert.Equal("one", change.OldItem);
        Assert.Equal(1, change.OldIndex);
    }

    [Fact]
    public void Replace()
    {
        var change = ListChange.Replace(1, "one", "two");

        Assert.Equal(ListChangeAction.Replace, change.Action);
        Assert.Equal("two", change.NewItem);
        Assert.Equal(1, change.NewIndex);
        Assert.Equal("one", change.OldItem);
        Assert.Equal(1, change.OldIndex);
    }

    [Fact]
    public void Move()
    {
        var change = ListChange.Move(1, 2, "one");

        Assert.Equal(ListChangeAction.Move, change.Action);
        Assert.Equal("one", change.NewItem);
        Assert.Equal(2, change.NewIndex);
        Assert.Equal("one", change.OldItem);
        Assert.Equal(1, change.OldIndex);
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