using System.ComponentModel;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Data;
using Xunit;

namespace Kinetic.Data.Tests;

public class KineticBindingTests
{
    static KineticBindingTests() =>
        TypeDescriptor.AddProvider(new KineticTypeDescriptorProvider(), typeof(ObservableObject));

    [StaFact]
    public void BindingToProperty()
    {
        var child = new Child("foo");
        var parent = new Parent(child);
        var target = new TextBox { DataContext = parent };
        var binding = new Binding
        {
            Path = new("Child.Text"),
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
        };

        target.SetBinding(TextBox.TextProperty, binding);

        Assert.Equal("foo", target.Text);

        child.Text.Set("bar");
        Assert.Equal("bar", target.Text);

        parent.Child.Set(child = new Child("baz"));
        Assert.Equal("baz", target.Text);

        target.Text = "boo";
        Assert.Equal("boo", child.Text);

        parent.Child.Set(null);
        Assert.Equal("", target.Text);

        target.DataContext = "Wrong object";
        Assert.Equal("", target.Text);

        target.DataContext = parent;
        Assert.Equal("", target.Text);

        parent.Child.Set(child);
        Assert.Equal("boo", target.Text);
    }

    [StaFact]
    public void BindingToReadOnlyProperty()
    {
        var child = new Child("foo");
        var parent = new Parent(child);
        var target = new TextBox { DataContext = parent };
        var binding = new Binding
        {
            Path = new("Child.TextReadOnly"),
            Mode = BindingMode.OneWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
        };

        target.SetBinding(TextBox.TextProperty, binding);

        Assert.Equal("foo", target.Text);

        child.Text.Set("bar");
        Assert.Equal("bar", target.Text);

        parent.Child.Set(child = new Child("baz"));
        Assert.Equal("baz", target.Text);

        parent.Child.Set(null);
        Assert.Equal("", target.Text);

        target.DataContext = "Wrong object";
        Assert.Equal("", target.Text);

        target.DataContext = parent;
        Assert.Equal("", target.Text);

        parent.Child.Set(child);
        Assert.Equal("baz", target.Text);
    }

    [StaFact]
    public void BindingToList()
    {
        var container = new Container();
        var target = new ItemsControl { DataContext = container };
        var binding = new Binding { Path = new("Items") };

        target.SetBinding(ItemsControl.ItemsSourceProperty, binding);

        Assert.Null(target.ItemsSource);

        var listOdd = new ObservableList<int>() { 1, 3, 5 };
        var listEven = new ObservableList<int>() { 2, 4, 8 };

        container.Items.Set(listOdd);

        Assert.Equal(listOdd, target.ItemsSource);
        Assert.Equal(listOdd.Cast<object>(), target.Items.Cast<object>());

        listOdd.RemoveAt(0);
        listOdd.Add(7);

        Assert.Equal(listOdd.Cast<object>(), target.Items.Cast<object>());

        container.Items.Set(listEven);

        Assert.Equal(listEven, target.ItemsSource);
        Assert.Equal(listEven.Cast<object>(), target.Items.Cast<object>());

        listEven.Move(0, 2);
        listEven.Move(1, 0);

        Assert.Equal(listEven.Cast<object>(), target.Items.Cast<object>());
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
        public ReadOnlyProperty<string?> TextReadOnly => Text;
    }

    private sealed class Container : ObservableObject
    {
        private ObservableList<int>? _items;
        public Property<ObservableList<int>?> Items => Property(ref _items);
    }
}