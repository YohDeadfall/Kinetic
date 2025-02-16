using System;
using System.Linq;
using Xunit;

namespace Kinetic.Linq.Tests;

public class ObservableViewTests
{
    [Fact]
    public void GroupByStaticWithoutComparer()
    {
        var list = new ObservableList<Item>();
        var groupings = list.GroupBy(item => item.Name.Get()).ToView();
        var groupingsOrdered = list.GroupBy(item => item.Name.Get(), items => items.OrderBy(item => item.Number)).ToView();

        var itemA = new Item(0, "A");
        var itemB = new Item(1, "B");
        var itemC = new Item(2, "A");
        var itemD = new Item(3, "B");

        list.Add(itemB);
        list.Add(itemC);
        list.Add(itemD);
        list.Add(itemA);

        var aGrouping = groupings.First(g => g.Key == "A");
        var bGrouping = groupings.First(g => g.Key == "B");
        var aGroupingOrdered = groupingsOrdered.First(g => g.Key == "A");
        var bGroupingOrdered = groupingsOrdered.First(g => g.Key == "B");

        Assert.Equal(new[] { bGrouping, aGrouping }, groupings);
        Assert.Equal(new[] { itemC, itemA }, aGrouping);
        Assert.Equal(new[] { itemB, itemD }, bGrouping);

        Assert.Equal(new[] { bGroupingOrdered, aGroupingOrdered }, groupingsOrdered);
        Assert.Equal(new[] { itemA, itemC }, aGroupingOrdered);
        Assert.Equal(new[] { itemB, itemD }, bGroupingOrdered);

        list.Move(1, 0);

        Assert.Equal(new[] { bGrouping, aGrouping }, groupings);
        Assert.Equal(new[] { itemC, itemA }, aGrouping);
        Assert.Equal(new[] { itemB, itemD }, bGrouping);

        Assert.Equal(new[] { bGroupingOrdered, aGroupingOrdered }, groupingsOrdered);
        Assert.Equal(new[] { itemA, itemC }, aGroupingOrdered);
        Assert.Equal(new[] { itemB, itemD }, bGroupingOrdered);

        itemA.Number.Set(5);
        itemB.Number.Set(4);

        Assert.Equal(new[] { bGrouping, aGrouping }, groupings);
        Assert.Equal(new[] { itemC, itemA }, aGrouping);
        Assert.Equal(new[] { itemB, itemD }, bGrouping);

        Assert.Equal(new[] { bGroupingOrdered, aGroupingOrdered }, groupingsOrdered);
        Assert.Equal(new[] { itemC, itemA }, aGroupingOrdered);
        Assert.Equal(new[] { itemD, itemB }, bGroupingOrdered);

        list.Remove(itemA);
        list.Remove(itemD);

        Assert.Equal(new[] { bGrouping, aGrouping }, groupings);
        Assert.Equal(new[] { itemC }, aGrouping);
        Assert.Equal(new[] { itemB }, bGrouping);

        Assert.Equal(new[] { bGroupingOrdered, aGroupingOrdered }, groupingsOrdered);
        Assert.Equal(new[] { itemC }, aGroupingOrdered);
        Assert.Equal(new[] { itemB }, bGroupingOrdered);

        list[0] = itemD;

        Assert.Equal(new[] { bGrouping }, groupings);
        Assert.Equal(new[] { itemB, itemD }, bGrouping);

        Assert.Equal(new[] { bGroupingOrdered }, groupingsOrdered);
        Assert.Equal(new[] { itemD, itemB }, bGroupingOrdered);

        list.Clear();

        Assert.Empty(groupings);
        Assert.Empty(groupingsOrdered);
    }

    [Fact]
    public void GroupByStaticWithComparer()
    {
        var list = new ObservableList<Item>();
        var groupings = list.GroupBy(item => item.Name.Get(), StringComparer.OrdinalIgnoreCase).ToView();
        var groupingsOrdered = list.GroupBy(item => item.Name.Get(), items => items.OrderBy(item => item.Number), StringComparer.OrdinalIgnoreCase).ToView();

        var itemA = new Item(0, "A");
        var itemB = new Item(1, "B");
        var itemC = new Item(2, "a");
        var itemD = new Item(3, "b");

        list.Add(itemB);
        list.Add(itemC);
        list.Add(itemD);
        list.Add(itemA);

        var aGrouping = groupings.First(g => g.Key == "a");
        var bGrouping = groupings.First(g => g.Key == "B");
        var aGroupingOrdered = groupingsOrdered.First(g => g.Key == "a");
        var bGroupingOrdered = groupingsOrdered.First(g => g.Key == "B");

        Assert.Equal(new[] { bGrouping, aGrouping }, groupings);
        Assert.Equal(new[] { itemC, itemA }, aGrouping);
        Assert.Equal(new[] { itemB, itemD }, bGrouping);

        Assert.Equal(new[] { bGroupingOrdered, aGroupingOrdered }, groupingsOrdered);
        Assert.Equal(new[] { itemA, itemC }, aGroupingOrdered);
        Assert.Equal(new[] { itemB, itemD }, bGroupingOrdered);

        list.Move(1, 0);

        Assert.Equal(new[] { bGrouping, aGrouping }, groupings);
        Assert.Equal(new[] { itemC, itemA }, aGrouping);
        Assert.Equal(new[] { itemB, itemD }, bGrouping);

        Assert.Equal(new[] { bGroupingOrdered, aGroupingOrdered }, groupingsOrdered);
        Assert.Equal(new[] { itemA, itemC }, aGroupingOrdered);
        Assert.Equal(new[] { itemB, itemD }, bGroupingOrdered);

        itemA.Number.Set(5);
        itemB.Number.Set(4);

        Assert.Equal(new[] { bGrouping, aGrouping }, groupings);
        Assert.Equal(new[] { itemC, itemA }, aGrouping);
        Assert.Equal(new[] { itemB, itemD }, bGrouping);

        Assert.Equal(new[] { bGroupingOrdered, aGroupingOrdered }, groupingsOrdered);
        Assert.Equal(new[] { itemC, itemA }, aGroupingOrdered);
        Assert.Equal(new[] { itemD, itemB }, bGroupingOrdered);

        list.Remove(itemA);
        list.Remove(itemD);

        Assert.Equal(new[] { bGrouping, aGrouping }, groupings);
        Assert.Equal(new[] { itemC }, aGrouping);
        Assert.Equal(new[] { itemB }, bGrouping);

        Assert.Equal(new[] { bGroupingOrdered, aGroupingOrdered }, groupingsOrdered);
        Assert.Equal(new[] { itemC }, aGroupingOrdered);
        Assert.Equal(new[] { itemB }, bGroupingOrdered);

        list[0] = itemD;

        Assert.Equal(new[] { bGrouping }, groupings);
        Assert.Equal(new[] { itemB, itemD }, bGrouping);

        Assert.Equal(new[] { bGroupingOrdered }, groupingsOrdered);
        Assert.Equal(new[] { itemD, itemB }, bGroupingOrdered);

        list.Clear();

        Assert.Empty(groupings);
        Assert.Empty(groupingsOrdered);
    }

    [Fact]
    public void GroupByDynamicWithoutComparer()
    {
        var list = new ObservableList<Item>();
        var groupings = list.GroupBy(item => item.Name).ToView();
        var groupingsOrdered = list.GroupBy(item => item.Name, items => items.OrderBy(item => item.Number)).ToView();

        var itemA = new Item(0, "A");
        var itemB = new Item(1, "B");
        var itemC = new Item(2, "A");
        var itemD = new Item(3, "B");

        list.Add(itemB);
        list.Add(itemC);
        list.Add(itemD);
        list.Add(itemA);

        var aGrouping = groupings.First(g => g.Key == "A");
        var bGrouping = groupings.First(g => g.Key == "B");
        var aGroupingOrdered = groupingsOrdered.First(g => g.Key == "A");
        var bGroupingOrdered = groupingsOrdered.First(g => g.Key == "B");

        Assert.Equal(new[] { bGrouping, aGrouping }, groupings);
        Assert.Equal(new[] { itemC, itemA }, aGrouping);
        Assert.Equal(new[] { itemB, itemD }, bGrouping);

        Assert.Equal(new[] { bGroupingOrdered, aGroupingOrdered }, groupingsOrdered);
        Assert.Equal(new[] { itemA, itemC }, aGroupingOrdered);
        Assert.Equal(new[] { itemB, itemD }, bGroupingOrdered);

        list.Move(1, 0);

        Assert.Equal(new[] { bGrouping, aGrouping }, groupings);
        Assert.Equal(new[] { itemC, itemA }, aGrouping);
        Assert.Equal(new[] { itemB, itemD }, bGrouping);

        Assert.Equal(new[] { bGroupingOrdered, aGroupingOrdered }, groupingsOrdered);
        Assert.Equal(new[] { itemA, itemC }, aGroupingOrdered);
        Assert.Equal(new[] { itemB, itemD }, bGroupingOrdered);

        itemA.Number.Set(5);
        itemB.Number.Set(4);

        Assert.Equal(new[] { bGrouping, aGrouping }, groupings);
        Assert.Equal(new[] { itemC, itemA }, aGrouping);
        Assert.Equal(new[] { itemB, itemD }, bGrouping);

        Assert.Equal(new[] { bGroupingOrdered, aGroupingOrdered }, groupingsOrdered);
        Assert.Equal(new[] { itemC, itemA }, aGroupingOrdered);
        Assert.Equal(new[] { itemD, itemB }, bGroupingOrdered);

        list.Remove(itemA);
        list.Remove(itemD);

        Assert.Equal(new[] { bGrouping, aGrouping }, groupings);
        Assert.Equal(new[] { itemC }, aGrouping);
        Assert.Equal(new[] { itemB }, bGrouping);

        Assert.Equal(new[] { bGroupingOrdered, aGroupingOrdered }, groupingsOrdered);
        Assert.Equal(new[] { itemC }, aGroupingOrdered);
        Assert.Equal(new[] { itemB }, bGroupingOrdered);

        list[0] = itemD;

        Assert.Equal(new[] { bGrouping }, groupings);
        Assert.Equal(new[] { itemB, itemD }, bGrouping);

        Assert.Equal(new[] { bGroupingOrdered }, groupingsOrdered);
        Assert.Equal(new[] { itemD, itemB }, bGroupingOrdered);

        itemD.Name.Set("A");

        aGrouping = groupings.First(g => g.Key == "A");
        aGroupingOrdered = groupingsOrdered.First(g => g.Key == "A");

        Assert.Equal(new[] { bGrouping, aGrouping }, groupings);
        Assert.Equal(new[] { itemD }, aGrouping);
        Assert.Equal(new[] { itemB }, bGrouping);

        Assert.Equal(new[] { bGroupingOrdered, aGroupingOrdered }, groupingsOrdered);
        Assert.Equal(new[] { itemD }, aGroupingOrdered);
        Assert.Equal(new[] { itemB }, bGroupingOrdered);

        list.Clear();

        Assert.Empty(groupings);
        Assert.Empty(groupingsOrdered);
    }

    [Fact]
    public void GroupByDynamicWithComparer()
    {
        var list = new ObservableList<Item>();
        var groupings = list.GroupBy(item => item.Name, StringComparer.OrdinalIgnoreCase).ToView();
        var groupingsOrdered = list.GroupBy(item => item.Name, items => items.OrderBy(item => item.Number), StringComparer.OrdinalIgnoreCase).ToView();

        var itemA = new Item(0, "a");
        var itemB = new Item(1, "B");
        var itemC = new Item(2, "A");
        var itemD = new Item(3, "b");

        list.Add(itemB);
        list.Add(itemC);
        list.Add(itemD);
        list.Add(itemA);

        var aGrouping = groupings.First(g => g.Key == "A");
        var bGrouping = groupings.First(g => g.Key == "B");
        var aGroupingOrdered = groupingsOrdered.First(g => g.Key == "A");
        var bGroupingOrdered = groupingsOrdered.First(g => g.Key == "B");

        Assert.Equal(new[] { bGrouping, aGrouping }, groupings);
        Assert.Equal(new[] { itemC, itemA }, aGrouping);
        Assert.Equal(new[] { itemB, itemD }, bGrouping);

        Assert.Equal(new[] { bGroupingOrdered, aGroupingOrdered }, groupingsOrdered);
        Assert.Equal(new[] { itemA, itemC }, aGroupingOrdered);
        Assert.Equal(new[] { itemB, itemD }, bGroupingOrdered);

        list.Move(1, 0);

        Assert.Equal(new[] { bGrouping, aGrouping }, groupings);
        Assert.Equal(new[] { itemC, itemA }, aGrouping);
        Assert.Equal(new[] { itemB, itemD }, bGrouping);

        Assert.Equal(new[] { bGroupingOrdered, aGroupingOrdered }, groupingsOrdered);
        Assert.Equal(new[] { itemA, itemC }, aGroupingOrdered);
        Assert.Equal(new[] { itemB, itemD }, bGroupingOrdered);

        itemA.Number.Set(5);
        itemB.Number.Set(4);

        Assert.Equal(new[] { bGrouping, aGrouping }, groupings);
        Assert.Equal(new[] { itemC, itemA }, aGrouping);
        Assert.Equal(new[] { itemB, itemD }, bGrouping);

        Assert.Equal(new[] { bGroupingOrdered, aGroupingOrdered }, groupingsOrdered);
        Assert.Equal(new[] { itemC, itemA }, aGroupingOrdered);
        Assert.Equal(new[] { itemD, itemB }, bGroupingOrdered);

        list.Remove(itemA);
        list.Remove(itemD);

        Assert.Equal(new[] { bGrouping, aGrouping }, groupings);
        Assert.Equal(new[] { itemC }, aGrouping);
        Assert.Equal(new[] { itemB }, bGrouping);

        Assert.Equal(new[] { bGroupingOrdered, aGroupingOrdered }, groupingsOrdered);
        Assert.Equal(new[] { itemC }, aGroupingOrdered);
        Assert.Equal(new[] { itemB }, bGroupingOrdered);

        list[0] = itemD;

        Assert.Equal(new[] { bGrouping }, groupings);
        Assert.Equal(new[] { itemB, itemD }, bGrouping);

        Assert.Equal(new[] { bGroupingOrdered }, groupingsOrdered);
        Assert.Equal(new[] { itemD, itemB }, bGroupingOrdered);

        itemD.Name.Set("A");

        aGrouping = groupings.First(g => g.Key == "A");
        aGroupingOrdered = groupingsOrdered.First(g => g.Key == "A");

        Assert.Equal(new[] { bGrouping, aGrouping }, groupings);
        Assert.Equal(new[] { itemD }, aGrouping);
        Assert.Equal(new[] { itemB }, bGrouping);

        Assert.Equal(new[] { bGroupingOrdered, aGroupingOrdered }, groupingsOrdered);
        Assert.Equal(new[] { itemD }, aGroupingOrdered);
        Assert.Equal(new[] { itemB }, bGroupingOrdered);

        list.Clear();

        Assert.Empty(groupings);
        Assert.Empty(groupingsOrdered);
    }

    [Fact]
    public void OnItemAdded()
    {
        var list = new ObservableList<int>();
        var handledBefore = false;
        var handledAfter = false;

        using var changes = list
            .Changed
            .Do(change => SetWhenAdded(change, ref handledBefore))
            .OnItemAdded(item =>
            {
                Assert.True(handledBefore);
                Assert.False(handledAfter);
            })
            .Do(change => SetWhenAdded(change, ref handledAfter))
            .Subscribe();

        list.Add(0);

        Assert.True(handledBefore);
        Assert.True(handledAfter);

        static void SetWhenAdded<T>(ListChange<T> change, ref bool flag)
        {
            if (change.Action is ListChangeAction.Insert or ListChangeAction.Replace)
                flag = true;
        }
    }

    [Fact]
    public void OnItemRemoved()
    {
        var list = new ObservableList<int>();
        var handledBefore = false;
        var handledAfter = false;

        using var changes = list
            .Changed
            .Do(change => SetWhenRemoved(change, ref handledBefore))
            .OnItemRemoved(item =>
            {
                Assert.True(handledBefore);
                Assert.True(handledAfter);
            })
            .Do(change => SetWhenRemoved(change, ref handledAfter))
            .Subscribe();

        list.Add(0);
        list.RemoveAt(0);

        Assert.True(handledBefore);
        Assert.True(handledAfter);

        static void SetWhenRemoved<T>(ListChange<T> change, ref bool flag)
        {
            if (change.Action is ListChangeAction.Remove or ListChangeAction.RemoveAll or ListChangeAction.Replace)
                flag = true;
        }
    }

    [Fact]
    public void OrderByStatic()
    {
        var list = new ObservableList<Item>();
        var view = list.OrderBy(item => item.Number.Get()).ToView();

        var itemA = new Item(0, "A");
        var itemB = new Item(1, "B");
        var itemC = new Item(2, "C");
        var itemD = new Item(3, "D");

        list.Add(itemA);
        list.Add(itemC);
        list.Add(itemD);
        list.Add(itemB);

        Assert.Equal(new[] { itemA, itemC, itemD, itemB }, list);
        Assert.Equal(new[] { itemA, itemB, itemC, itemD }, view);

        itemA.Number.Set(5);
        itemB.Number.Set(4);

        Assert.Equal(new[] { itemA, itemC, itemD, itemB }, list);
        Assert.Equal(new[] { itemA, itemB, itemC, itemD }, view);

        list.Remove(itemA);
        list.Remove(itemD);

        Assert.Equal(new[] { itemC, itemB }, list);
        Assert.Equal(new[] { itemB, itemC }, view);

        list[1] = itemD;

        Assert.Equal(new[] { itemC, itemD }, list);
        Assert.Equal(new[] { itemC, itemD }, view);

        list.Move(1, 0);

        Assert.Equal(new[] { itemD, itemC }, list);
        Assert.Equal(new[] { itemC, itemD }, view);

        list.Clear();

        Assert.Empty(list);
        Assert.Empty(view);

        list.Add(itemA);
        list.Add(itemC);
        list.Add(itemD);
        list.Add(itemB);

        Assert.Equal(new[] { itemA, itemC, itemD, itemB }, list);
        Assert.Equal(new[] { itemC, itemD, itemB, itemA }, view);
    }

    [Fact]
    public void OrderByDynamic()
    {
        var list = new ObservableList<Item>();
        var view = list.OrderBy(item => item.Number).ToView();

        var itemA = new Item(0, "A");
        var itemB = new Item(1, "B");
        var itemC = new Item(2, "C");
        var itemD = new Item(3, "D");

        list.Add(itemA);
        list.Add(itemC);
        list.Add(itemD);
        list.Add(itemB);

        Assert.Equal(new[] { itemA, itemC, itemD, itemB }, list);
        Assert.Equal(new[] { itemA, itemB, itemC, itemD }, view);

        itemA.Number.Set(5);
        itemC.Number.Set(0);

        Assert.Equal(new[] { itemA, itemC, itemD, itemB }, list);
        Assert.Equal(new[] { itemC, itemB, itemD, itemA }, view);

        list.Remove(itemA);
        list.Remove(itemD);

        Assert.Equal(new[] { itemC, itemB }, list);
        Assert.Equal(new[] { itemC, itemB }, view);

        list[1] = itemD;

        Assert.Equal(new[] { itemC, itemD }, list);
        Assert.Equal(new[] { itemC, itemD }, view);

        list.Move(1, 0);

        Assert.Equal(new[] { itemD, itemC }, list);
        Assert.Equal(new[] { itemC, itemD }, view);

        list.Clear();

        Assert.Empty(list);
        Assert.Empty(view);

        list.Add(itemA);
        list.Add(itemC);
        list.Add(itemD);
        list.Add(itemB);

        Assert.Equal(new[] { itemA, itemC, itemD, itemB }, list);
        Assert.Equal(new[] { itemC, itemB, itemD, itemA }, view);
    }

    [Fact]
    public void SelectStatic()
    {
        var list = new ObservableList<Item>();
        var view = list.Select(item => item.Name.Get()).ToView();

        var itemA = new Item(0, "A");
        var itemB = new Item(1, "B");
        var itemC = new Item(2, "C");
        var itemD = new Item(3, "D");

        list.Add(itemA);
        list.Add(itemC);
        list.Add(itemD);
        list.Add(itemB);

        Assert.Equal(new[] { "A", "C", "D", "B" }, view);

        list.Remove(itemA);
        list.Remove(itemB);

        Assert.Equal(new[] { "C", "D" }, view);

        list[1] = itemB;

        Assert.Equal(new[] { "C", "B" }, view);

        list.Move(1, 0);

        Assert.Equal(new[] { "B", "C" }, view);

        list.Clear();

        Assert.Empty(view);
    }

    [Fact]
    public void SelectDynamic()
    {
        var list = new ObservableList<Item>();
        var view = list.SelectAwait(item => item.Name).ToView();

        var itemA = new Item(0, "A");
        var itemB = new Item(1, "B");
        var itemC = new Item(2, "C");
        var itemD = new Item(3, "D");

        list.Add(itemA);
        list.Add(itemC);
        list.Add(itemD);
        list.Add(itemB);

        Assert.Equal(new[] { "A", "C", "D", "B" }, view);

        itemA.Name.Set("X");
        itemB.Name.Set("Y");
        itemC.Name.Set("Z");
        itemD.Name.Set("W");

        list.Remove(itemA);
        list.Remove(itemB);

        Assert.Equal(new[] { "Z", "W" }, view);

        list[1] = itemB;

        Assert.Equal(new[] { "Z", "Y" }, view);

        list.Move(1, 0);

        Assert.Equal(new[] { "Y", "Z" }, view);

        list.Clear();

        Assert.Empty(view);
    }

    [Fact]
    public void WhereStatic()
    {
        var list = new ObservableList<Item>();
        var view = list.Where(item => item.Number % 2 == 0).ToView();

        var itemA = new Item(0, "A");
        var itemB = new Item(1, "B");
        var itemC = new Item(2, "C");
        var itemD = new Item(3, "D");

        list.Add(itemA);
        list.Add(itemB);
        list.Add(itemC);
        list.Add(itemD);

        Assert.Equal(new[] { itemA, itemC }, view);

        itemA.Number.Set(5);
        itemB.Number.Set(4);

        Assert.Equal(new[] { itemA, itemC }, view);

        list.Remove(itemA);
        list.Remove(itemB);

        Assert.Equal(new[] { itemC }, view);

        list[1] = itemB;

        Assert.Equal(new[] { itemC, itemB }, view);

        list.Move(1, 0);

        Assert.Equal(new[] { itemB, itemC }, view);

        list.Clear();

        Assert.Empty(view);
    }

    [Fact]
    public void WhereDynamic()
    {
        var list = new ObservableList<Item>();
        var view = list.Where(item => item.Number.Changed.Select(static number => number % 2 == 0)).ToView();

        var itemA = new Item(0, "A");
        var itemB = new Item(1, "B");
        var itemC = new Item(2, "C");
        var itemD = new Item(3, "D");

        list.Add(itemA);
        list.Add(itemB);
        list.Add(itemC);
        list.Add(itemD);

        Assert.Equal(new[] { itemA, itemC }, view);

        itemA.Number.Set(5);
        itemB.Number.Set(4);

        Assert.Equal(new[] { itemB, itemC }, view);

        list.Remove(itemA);
        list.Remove(itemB);

        Assert.Equal(new[] { itemC }, view);

        list[1] = itemB;

        Assert.Equal(new[] { itemC, itemB }, view);

        list.Move(1, 0);

        Assert.Equal(new[] { itemB, itemC }, view);

        list.Clear();

        Assert.Empty(view);
    }

    private sealed class Item : ObservableObject
    {
        private int _number;
        private string _name;

        public Item(int number, string name)
        {
            _number = number;
            _name = name;
        }

        public Property<int> Number => Property(ref _number);
        public Property<string> Name => Property(ref _name);

        public override string ToString() =>
            $"({_number}:{_name})";
    }
}