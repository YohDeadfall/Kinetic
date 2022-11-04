using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Data.Core;
using Avalonia.Logging;
using Xunit;

namespace Kinetic.Data.Tests;

public class KineticPropertyAccessorTests
{
    static KineticPropertyAccessorTests() =>
        ExpressionObserver.PropertyAccessors.Insert(2, new KineticPropertyAccessor());

    [Fact]
    public void BindingToProperty()
    {
        var source = new Source();
        var target = new TextBox { DataContext = source };
        var binding = new Avalonia.Data.Binding { Path = "Text" };

        target.Bind(TextBox.TextProperty, binding);

        source.Text.Set("foo");
        Assert.Equal("foo", target.Text);

        source.Text.Set("bar");
        Assert.Equal("bar", target.Text);

        target.Text = "baz";
        Assert.Equal("baz", source.Text);
    }

    [Fact]
    public void BindingToReadOnlyProperty()
    {
        var source = new Source();
        var target = new TextBox { DataContext = source };
        var binding = new Avalonia.Data.Binding { Path = "TextReadOnly" };

        target.Bind(TextBox.TextProperty, binding);

        source.SetTextReadOnly("foo");
        Assert.Equal("foo", target.Text);

        source.SetTextReadOnly("bar");
        Assert.Equal("bar", target.Text);

        target.Text = "baz";
        Assert.Equal("bar", source.TextReadOnly);
    }

    [Fact]
    public void BindingToList()
    {
        var source = new Source();
        var target = new ItemsPresenter { DataContext = source };
        var binding = new Avalonia.Data.Binding { Path = "Items" };

        target.ApplyTemplate();
        target.Bind(ItemsRepeater.ItemsProperty, binding);

        Assert.Equal(source.Items, target.Items);

        source.Items.Add("foo");
        source.Items.Add("bar");

        Assert.Equal("foo", ((ContentPresenter) target.Panel.Children[0]).Content);
        Assert.Equal("bar", ((ContentPresenter) target.Panel.Children[1]).Content);
    }

    private sealed class Source : ObservableObject
    {
        private string? _text;
        private string? _textReadOnly;

        public Property<string?> Text => Property(ref _text);
        public ReadOnlyProperty<string?> TextReadOnly => Property(ref _textReadOnly);

        public ObservableList<string> Items { get; } = new();

        public void SetTextReadOnly(string value) => Set(TextReadOnly, value);
    }
}