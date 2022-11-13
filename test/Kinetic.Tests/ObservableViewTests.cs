using Xunit;

namespace Kinetic.Linq.Tests;

public class ObservableViewTests
{
    [Fact]
    public void Where()
    {
        var list = new ObservableList<Item>();
        var view = list.Where(item => item.Number.Changed.Select(static number => number % 2 == 0)).ToView();

        var itemA = new Item(0);
        var itemB = new Item(1);
        var itemC = new Item(2);
        var itemD = new Item(3);

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

        public Item(int number) => Number.Set(number);

        public Property<int> Number => Property(ref _number);
    }
}