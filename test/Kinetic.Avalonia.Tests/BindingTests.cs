using Avalonia;
using Avalonia.Controls;
using Kinetic.Linq;
using Xunit;

namespace Kinetic.Data.Tests;

public class BindingTests
{
    [Fact]
    public void BindingToProperty()
    {
        var child = new Child("foo");
        var parent = new Parent(child);
        var target = new TextBox { DataContext = parent };

        target.Bind(
            TextBox.TextProperty,
            Binding.TwoWay(source => source
                .Select(source => (Parent?) source)
                .Property(source => source?.Child)
                .Property(source => source?.Text)));

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

    private sealed class Parent : ObservableObject
    {
        private Child? _child;

        public Parent(Child? child) => _child = child;
        public Property<Child?> Child => Property(ref _child);
    }

    public sealed class Child : ObservableObject
    {
        private string? _text;

        public Child(string? text) => _text = text;
        public Property<string?> Text => Property(ref _text);
    }
}