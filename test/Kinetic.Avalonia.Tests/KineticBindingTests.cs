using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless.XUnit;
using Kinetic.Linq;
using Xunit;

namespace Kinetic.Data.Tests;

public class KineticBindingTests
{
    [AvaloniaFact]
    public void BindingToProperty()
    {
        var child = new Child("foo");
        var parent = new Parent(child);
        var target = new TextBox { DataContext = parent };

        target.BindTwoWay(
            TextBox.TextProperty,
            source => source
                .Select(source => source as Parent)
                .Property(source => source?.Child)
                .Property(source => source?.Text));

        Assert.Equal("foo", target.Text);

        child.Text.Set("bar");
        Assert.Equal("bar", target.Text);

        parent.Child.Set(child = new Child("baz"));
        Assert.Equal("baz", target.Text);

        target.Text = "boo";
        Assert.Equal("boo", child.Text);

        parent.Child.Set(null);
        Assert.Null(target.Text);

        target.DataContext = "Wrong object";
        Assert.Null(target.Text);

        target.DataContext = parent;
        Assert.Null(target.Text);

        parent.Child.Set(child);
        Assert.Equal("boo", target.Text);
    }

    [AvaloniaFact]
    public void BindingToList()
    {
        var container = new Container();
        var target = new ItemsControl { DataContext = container };
        var window = new Window { Content = target };

        window.Show();
        target.BindOneWay(
            ItemsControl.ItemsSourceProperty,
            source => source
                .Select(source => (Container?) source)
                .Property(source => source?.Items));

        Assert.Null(target.ItemsSource);

        var listOdd = new ObservableList<int>() { 1, 3, 5 };
        var listEven = new ObservableList<int>() { 2, 4, 8 };

        container.Items.Set(listOdd);

        Assert.Equal(listOdd, target.ItemsSource);
        Assert.Equal(1, ((ContentPresenter?) target.ContainerFromIndex(0))?.Content);
        Assert.Equal(3, ((ContentPresenter?) target.ContainerFromIndex(1))?.Content);
        Assert.Equal(5, ((ContentPresenter?) target.ContainerFromIndex(2))?.Content);

        listOdd.RemoveAt(0);
        listOdd.Add(7);

        Assert.Equal(3, ((ContentPresenter?) target.ContainerFromIndex(0))?.Content);
        Assert.Equal(5, ((ContentPresenter?) target.ContainerFromIndex(1))?.Content);
        Assert.Equal(7, ((ContentPresenter?) target.ContainerFromIndex(2))?.Content);

        container.Items.Set(listEven);

        Assert.Equal(listEven, target.ItemsSource);
        Assert.Equal(2, ((ContentPresenter?) target.ContainerFromIndex(0))?.Content);
        Assert.Equal(4, ((ContentPresenter?) target.ContainerFromIndex(1))?.Content);
        Assert.Equal(8, ((ContentPresenter?) target.ContainerFromIndex(2))?.Content);

        listEven.Move(0, 2);
        listEven.Move(1, 0);

        Assert.Equal(8, ((ContentPresenter?) target.ContainerFromIndex(0))?.Content);
        Assert.Equal(4, ((ContentPresenter?) target.ContainerFromIndex(1))?.Content);
        Assert.Equal(2, ((ContentPresenter?) target.ContainerFromIndex(2))?.Content);
    }

    private sealed class Parent : ObservableObject
    {
        private Child? _child;

        public Parent(Child? child) => _child = child;
        public Property<Child?> Child => Property(ref _child);
    }

    private sealed class Child : ObservableObject
    {
        private string? _text;

        public Child(string? text) => _text = text;
        public Property<string?> Text => Property(ref _text);
    }

    private sealed class Container : ObservableObject
    {
        private ObservableList<int>? _items;
        public Property<ObservableList<int>?> Items => Property(ref _items);
    }
}