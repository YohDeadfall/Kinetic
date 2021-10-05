using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Kinetic.Linq.Tests
{
    public class LinqTests
    {
        [Fact]
        public async ValueTask AnyWithPredicate()
        {
            var source = new Source<int>();
            var task = source.Value.Changed
                .Any(value => value > 2)
                .ToValueTask();

            source.Value.Set(1);
            source.Value.Set(2);
            source.Value.Set(3);

            Assert.True(await task);
        }

        [Fact]
        public async ValueTask AnyWithoutPredicate()
        {
            var source = new Source<int>();
            var task = source.Value.Changed
                .Any()
                .ToValueTask();

            source.Value.Set(1);
            source.Value.Set(2);

            Assert.True(await task);
        }

        [Fact]
        public async ValueTask Contains()
        {
            var source = new Source<int>();
            var task = source.Value.Changed
                .Contains(2)
                .ToValueTask();

            source.Value.Set(1);
            source.Value.Set(2);

            Assert.True(await task);
        }

        [Fact]
        public void Distinct()
        {
            var source = new Source<int>();
            var values = new List<int>();

            source.Value.Changed
                .Distinct()
                .Subscribe(value => values.Add(value));

            source.Value.Set(1);
            source.Value.Set(1);
            source.Value.Set(2);
            source.Value.Set(2);

            Assert.Equal(new[] { 0, 1, 2 }, values);
        }

        [Fact]
        public void Skip()
        {
            var source = new Source<int>();
            var values = new List<int>();

            source.Value.Changed
                .Skip(2)
                .Subscribe(value => values.Add(value));

            source.Value.Set(1);
            source.Value.Set(2);
            source.Value.Set(3);

            Assert.Equal(new[] { 2, 3 }, values);
        }

        [Fact]
        public async ValueTask First()
        {
            var source = new Source<int>();
            var task = source.Value.Changed
                .First()
                .ToValueTask();

            source.Value.Set(1);
            source.Value.Set(2);

            Assert.Equal(0, await task);
        }

        [Fact]
        public async ValueTask FirstOrDefault()
        {
            var source = new Source<int>();
            var task = source.Value.Changed
                .FirstOrDefault()
                .ToValueTask();

            source.Value.Set(1);
            source.Value.Set(2);

            Assert.Equal(0, await task);
        }

        [Fact]
        public void SkipWhile()
        {
            var source = new Source<int>();
            var values = new List<int>();

            source.Value.Changed
                .SkipWhile(value => value < 2)
                .Subscribe(value => values.Add(value));

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
                .Subscribe(value => values.Add(value));

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
                .TakeWhile(value => value < 2)
                .Subscribe(value => values.Add(value));

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
                .Where(value => value > 2)
                .Subscribe(value => values.Add(value));

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