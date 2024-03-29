using Xunit;

namespace Kinetic.Linq.Tests;

public class ObservableViewTests
{
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
        itemB.Number.Set(4);

        Assert.Equal(new[] { itemA, itemC, itemD, itemB }, list);
        Assert.Equal(new[] { itemC, itemD, itemB, itemA }, view);

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
        Assert.Equal(new[] { itemC, itemD, itemB, itemA }, view);
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
        var view = list.Select(item => item.Name).ToView();

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