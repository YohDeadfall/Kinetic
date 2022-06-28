using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Core;
using Xunit;

namespace Kinetic.Data.Tests;

public class PropertyAccessorTests
{
    static PropertyAccessorTests() =>
        ExpressionObserver.PropertyAccessors.Insert(2, new PropertyAccessor());

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

    private sealed class Source : ObservableObject
    {
        private string _text = string.Empty;
        private string _textReadOnly = string.Empty;

        public Property<string> Text => Property(ref _text);
        public ReadOnlyProperty<string> TextReadOnly => Property(ref _textReadOnly);

        public void SetTextReadOnly(string value) => Set(TextReadOnly, value);
    }
}