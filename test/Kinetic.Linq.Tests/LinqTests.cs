using System.Collections.Generic;
using Xunit;

namespace Kinetic.Linq.Tests
{
    public class LinqTests
    {
        [Fact]
        public void AnyWithPredicate()
        {
            var executions = 0;
            var source = new Source<int>();

            source.Value.Changed
                .Any((int value) => value > 2)
                .Subscribe((bool value) =>
                {
                    executions += 1;
                    Assert.True(value);
                });

            source.Value.Set(1);
            source.Value.Set(2);
            source.Value.Set(3);

            Assert.Equal(1, executions);
        }

        [Fact]
        public void AnyWithoutPredicate()
        {
            var executions = 0;
            var source = new Source<int>();

            source.Value.Changed
                .Any()
                .Subscribe((bool value) =>
                {
                    executions += 1;
                    Assert.True(value);
                });

            source.Value.Set(1);
            source.Value.Set(2);

            Assert.Equal(1, executions);
        }

        [Fact]
        public void Contains()
        {
            var executions = 0;
            var source = new Source<int>();

            source.Value.Changed
                .Contains(2)
                .Subscribe((bool value) =>
                {
                    executions += 1;
                    Assert.True(value);
                });

            source.Value.Set(1);
            source.Value.Set(2);

            Assert.Equal(1, executions);
        }

        [Fact]
        public void Skip()
        {
            var source = new Source<int>();
            var values = new List<int>();

            source.Value.Changed
                .Skip(2)
                .Subscribe((int value) => values.Add(value));

            source.Value.Set(1);
            source.Value.Set(2);
            source.Value.Set(3);

            Assert.Equal(new[] { 2, 3 }, values);
        }

        [Fact]
        public void First()
        {
            var executions = 0;
            var source = new Source<int>();

            source.Value.Changed
                .First()
                .Subscribe((int value) =>
                {
                    executions += 1;
                    Assert.Equal(0, value);
                });

            source.Value.Set(1);
            source.Value.Set(2);

            Assert.Equal(1, executions);
        }

        [Fact]
        public void FirstOrDefault()
        {
            var executions = 0;
            var source = new Source<int>();

            source.Value.Changed
                .FirstOrDefault()
                .Subscribe((int value) =>
                {
                    executions += 1;
                    Assert.Equal(0, value);
                });

            source.Value.Set(1);
            source.Value.Set(2);

            Assert.Equal(1, executions);
        }

        [Fact]
        public void SkipWhile()
        {
            var source = new Source<int>();
            var values = new List<int>();

            source.Value.Changed
                .SkipWhile((int value) => value < 2)
                .Subscribe((int value) => values.Add(value));

            source.Value.Set(1);
            source.Value.Set(2);
            source.Value.Set(3);

            Assert.Equal(new[] { 2, 3 }, values);
        }

        [Fact]
        public void Take()
        {
            var source = new Source<int>();
            var values = new List<int>();

            source.Value.Changed
                .Take(2)
                .Subscribe((int value) => values.Add(value));

            source.Value.Set(1);
            source.Value.Set(2);
            source.Value.Set(3);

            Assert.Equal(new[] { 0, 1 }, values);
        }

        [Fact]
        public void TakeWhile()
        {
            var source = new Source<int>();
            var values = new List<int>();

            source.Value.Changed
                .TakeWhile((int value) => value < 2)
                .Subscribe((int value) => values.Add(value));

            source.Value.Set(1);
            source.Value.Set(2);
            source.Value.Set(3);

            Assert.Equal(new[] { 0, 1 }, values);
        }

        [Fact]
        public void Where()
        {
            var source = new Source<int>();
            var values = new List<int>();

            source.Value.Changed
                .Where((int value) => value > 2)
                .Subscribe((int value) => values.Add(value));

            source.Value.Set(1);
            source.Value.Set(3);
            source.Value.Set(2);
            source.Value.Set(4);

            Assert.Equal(new[] { 3, 4 }, values);
        }

        private sealed class Source<T> : Object
        {
            private T _value;

            public Source() => _value = default!;
            public Source(T value) => _value = value;
            public Property<T> Value => Property(ref _value);
        }
    }
}