using System;
using System.Collections.Generic;
using Xunit;

namespace Kinetic.Tests
{
    public class KineticObjectTests
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
                new Observer<int>(value => numbers.Add(value))))
            {
                test.Number.Set(1);
                test.Number.Set(2);
                test.Number.Set(42);
            }

            test.Number.Set(62);

            Assert.Equal(new[] { 0, 1, 2, 42 }, numbers);
        }

        private sealed class TestObject : KineticObject
        {
            private int _number;
            private string _text = string.Empty;

            public TestObject() =>
                Number.Changed.Subscribe(new NumberChanged(this));

            public KineticProperty<int> Number => Property(ref _number);
            public KineticReadOnlyProperty<string> Text => Property(ref _text);

            private class NumberChanged : IObserver<int>
            {
                public TestObject Owner { get; }
                public NumberChanged(TestObject owner) => Owner = owner;

                public void OnNext(int value) => Owner.Set(Owner.Text, value.ToString());
                public void OnError(Exception exception) { }
                public void OnCompleted() { }
            }
        }

        private sealed class Observer<T> : IObserver<T>
        {
            public Action<T> Handler { get; }
            public Observer(Action<T> handler) => Handler = handler;

            public void OnNext(T value) => Handler(value);
            public void OnError(Exception exception) { }
            public void OnCompleted() { }
        }
    }
}
