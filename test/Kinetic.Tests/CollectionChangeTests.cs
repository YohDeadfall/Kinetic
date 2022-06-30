using System;
using Xunit;

namespace Kinetic.Collections.Tests;

public class CollectionChangeTests
{
    [Fact]
    public void Insert()
    {
        var change = CollectionChange.Insert(1, "one");

        Assert.Equal(CollectionChangeAction.Insert, change.Action);
        Assert.Equal("one", change.NewItem);
        Assert.Equal(1, change.NewIndex);
        Assert.Throws<InvalidOperationException>(() => change.OldItem);
        Assert.Throws<InvalidOperationException>(() => change.OldIndex);
    }

    [Fact]
    public void Remove()
    {
        var change = CollectionChange.Remove(1, "one");

        Assert.Equal(CollectionChangeAction.Remove, change.Action);
        Assert.Throws<InvalidOperationException>(() => change.NewItem);
        Assert.Throws<InvalidOperationException>(() => change.NewIndex);
        Assert.Equal("one", change.OldItem);
        Assert.Equal(1, change.OldIndex);
    }

    [Fact]
    public void Replace()
    {
        var change = CollectionChange.Replace(1, "one", "two");

        Assert.Equal(CollectionChangeAction.Replace, change.Action);
        Assert.Equal("two", change.NewItem);
        Assert.Equal(1, change.NewIndex);
        Assert.Equal("one", change.OldItem);
        Assert.Equal(1, change.OldIndex);
    }

    [Fact]
    public void Move()
    {
        var change = CollectionChange.Move(1, 2, "one");

        Assert.Equal(CollectionChangeAction.Move, change.Action);
        Assert.Equal("one", change.NewItem);
        Assert.Equal(2, change.NewIndex);
        Assert.Equal("one", change.OldItem);
        Assert.Equal(1, change.OldIndex);
    }

    [Fact]
    public void Reset()
    {
        var change = CollectionChange.Reset<string>();

        Assert.Equal(default, change);
        Assert.Equal(CollectionChangeAction.Reset, change.Action);
        Assert.Throws<InvalidOperationException>(() => change.NewItem);
        Assert.Throws<InvalidOperationException>(() => change.NewIndex);
        Assert.Throws<InvalidOperationException>(() => change.OldItem);
        Assert.Throws<InvalidOperationException>(() => change.OldIndex);
    }
}