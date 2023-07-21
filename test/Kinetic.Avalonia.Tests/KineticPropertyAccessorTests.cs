using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Data.Core.Plugins;
using Avalonia.Headless.XUnit;
using Xunit;

namespace Kinetic.Data.Tests;

public class KineticPropertyAccessorTests
{
    static KineticPropertyAccessorTests() =>
        BindingPlugins.PropertyAccessors.Insert(2, new KineticPropertyAccessor());

    [AvaloniaFact]
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

    [AvaloniaFact]
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

    [AvaloniaFact]
    public void BindingToList()
    {
        var source = new Source();
        var target = new ItemsControl { DataContext = source };
        var window = new Window { Content = target };
        var binding = new Avalonia.Data.Binding { Path = "Items" };

        window.Show(); ;
        target.Bind(ItemsControl.ItemsSourceProperty, binding);

        Assert.Equal(source.Items, target.ItemsSource);

        source.Items.Add("foo");
        source.Items.Add("bar");

        Assert.Equal("foo", ((ContentPresenter?) target.ContainerFromIndex(0))?.Content);
        Assert.Equal("bar", ((ContentPresenter?) target.ContainerFromIndex(1))?.Content);
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