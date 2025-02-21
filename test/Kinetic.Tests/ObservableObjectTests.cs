using System;
using System.Collections.Generic;
using Kinetic.Linq;
using Xunit;

namespace Kinetic.Tests;

public class ObservableObjectTests
{
    [Fact]
    public void PropertyAccessors()
    {
        var test = new TestObject();

        Assert.Equal(0, test.Number);
        Assert.Equal("0", test.Text);

        test.Number.Set(42);

        Assert.Equal(42, test.Number);
        Assert.Equal("42", test.Text);
    }

    [Fact]
    public void PropertySubscription()
    {
        var test = new TestObject();
        var numbers = new List<int>();

        using (test.Number.Changed.Subscribe(
            value => numbers.Add(value)))
        {
            test.Number.Set(1);
            test.Number.Set(2);
            test.Number.Set(3);
        }

        test.Number.Set(4);

        Assert.Equal(new[] { 0, 1, 2, 3 }, numbers);
    }

    [Fact]
    public void PropertyChange()
    {
        var test = new TestObject();
        var numbers = new List<int>();

        using (test.Number.Changed.Subscribe(
            value => numbers.Add(value)))
        {
            test.Number.Change.OnNext(1);
            test.Number.Change.OnNext(2);
            test.Number.Change.OnNext(3);
        }

        test.Number.Set(4);

        Assert.Equal(new[] { 0, 1, 2, 3 }, numbers);
    }

    [Fact]
    public void SuppressNotifications()
    {
        var test = new TestObject();
        var numbers = new List<int>();

        test.Number.Changed.Subscribe(
            value => numbers.Add(value));

        using (test.SuppressNotifications())
        {
            test.Number.Set(1);
            test.Number.Set(2);
            test.Number.Set(3);
        }

        Assert.Equal(new[] { 0, 3 }, numbers);
    }

    [Fact]
    public void SetterUsesEqualityComparer()
    {
        var test = new TestObject();
        var numbers = new List<int>();

        test.Number.Changed.Subscribe(
            value => numbers.Add(value));

        test.Number.Set(0);
        test.Number.Set(1);
        test.Number.Set(1);

        Assert.Equal(new[] { 0, 1 }, numbers);
    }

    [Fact]
    public void SetterThrowsForAnotherObject()
    {
        var one = new TestObject();
        var two = new TestObject();

        Assert.Throws<ArgumentException>(() => two.SetProperty(one.Number, 1));
    }

    private sealed class TestObject : ObservableObject
    {
        private int _number;
        private string _text = string.Empty;

        public TestObject() =>
            Number.Changed.Subscribe(value => Set(Text, value.ToString()));

        public Property<int> Number => Property(ref _number);
        public ReadOnlyProperty<string> Text => Property(ref _text);

        public void SetProperty<T>(ReadOnlyProperty<T> property, T value) =>
            Set(property, value);
    }
}