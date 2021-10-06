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
            var task = source
                .Any(value => value > 2)
                .ToValueTask();

            source.Next(1);
            source.Next(2);
            source.Next(3);

            Assert.True(await task);
        }

        [Fact]
        public async ValueTask AnyWithoutPredicate()
        {
            var source = new Source<int>();
            var task = source
                .Any()
                .ToValueTask();

            source.Next(1);
            source.Next(2);

            Assert.True(await task);
        }

        [Fact]
        public async ValueTask Contains()
        {
            var source = new Source<int>();
            var task = source
                .Contains(2)
                .ToValueTask();

            source.Next(1);
            source.Next(2);

            Assert.True(await task);
        }

        [Fact]
        public void Distinct()
        {
            var source = new Source<int>();
            var values = new List<int>();

            source
                .Distinct()
                .Subscribe(value => values.Add(value));

            source.Next(1);
            source.Next(1);
            source.Next(2);
            source.Next(2);

            Assert.Equal(new[] { 1, 2 }, values);
        }

        [Fact]
        public void Skip()
        {
            var source = new Source<int>();
            var values = new List<int>();

            source
                .Skip(2)
                .Subscribe(value => values.Add(value));

            source.Next(0);
            source.Next(1);
            source.Next(2);
            source.Next(3);

            Assert.Equal(new[] { 2, 3 }, values);
        }

        [Fact]
        public async ValueTask First()
        {
            var source = new Source<int>();
            var task = source
                .First()
                .ToValueTask();

            source.Next(1);
            source.Next(2);

            Assert.Equal(1, await task);
        }

        [Fact]
        public async ValueTask FirstOrDefault()
        {
            var source = new Source<int>();
            var task = source
                .FirstOrDefault()
                .ToValueTask();

            source.Next(1);
            source.Next(2);

            Assert.Equal(1, await task);
        }

        [Fact]
        public void SkipWhile()
        {
            var source = new Source<int>();
            var values = new List<int>();

            source
                .SkipWhile(value => value < 2)
                .Subscribe(value => values.Add(value));

            source.Next(1);
            source.Next(2);
            source.Next(3);

            Assert.Equal(new[] { 2, 3 }, values);
        }

        [Fact]
        public void Take()
        {
            var source = new Source<int>();
            var values = new List<int>();

            source
                .Take(2)
                .Subscribe(value => values.Add(value));

            source.Next(1);
            source.Next(2);
            source.Next(3);

            Assert.Equal(new[] { 1, 2 }, values);
        }

        [Fact]
        public void TakeWhile()
        {
            var source = new Source<int>();
            var values = new List<int>();

            source
                .TakeWhile(value => value < 2)
                .Subscribe(value => values.Add(value));

            source.Next(0);
            source.Next(1);
            source.Next(2);
            source.Next(3);

            Assert.Equal(new[] { 0, 1 }, values);
        }

        [Fact]
        public void Where()
        {
            var source = new Source<int>();
            var values = new List<int>();

            source
                .Where(value => value > 2)
                .Subscribe(value => values.Add(value));

            source.Next(1);
            source.Next(3);
            source.Next(2);
            source.Next(4);

            Assert.Equal(new[] { 3, 4 }, values);
        }

        private sealed class Source<T> : Observable<T>
        {
            public void Next(T value) => OnNext(value);
        }
    }
}