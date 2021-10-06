using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Kinetic.Linq.Tests
{
    public class LinqTests
    {
        [Fact]
        public async ValueTask AllWithPredicate()
        {
            var source = new Source<int>();
            var task = source
                .Any(value => value < 4)
                .ToValueTask();

            source.Next(1);
            source.Next(2);
            source.Next(3);
            source.Complete();

            Assert.True(await task);
        }

        [Fact]
        public async ValueTask AllWithoutPredicate()
        {
            var source = new Source<bool>();
            var task = source
                .ToValueTask();

            source.Next(true);
            source.Next(true);
            source.Complete();

            Assert.True(await task);
        }

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
        public async ValueTask CountWithPredicate()
        {
            var source = new Source<int>();
            var task = source
                .Count(value => value > 2)
                .ToValueTask();

            source.Next(1);
            source.Next(2);
            source.Next(3);
            source.Complete();

            Assert.Equal(1, await task);
        }

        [Fact]
        public async ValueTask CountWithoutPredicate()
        {
            var source = new Source<int>();
            var task = source
                .ToValueTask();

            source.Next(1);
            source.Next(2);
            source.Next(3);
            source.Complete();

            Assert.Equal(3, await task);
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
        public async ValueTask FirstWithPredicate()
        {
            var source = new Source<int>();
            var task = source
                .First(value => value > 1)
                .ToValueTask();

            source.Next(1);
            source.Next(2);

            Assert.Equal(2, await task);
        }

        [Fact]
        public async ValueTask FirstWithoutPredicate()
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
        public async ValueTask FirstOrDefaultWithPredicate()
        {
            var source = new Source<int>();
            var task = source
                .FirstOrDefault(value => value > 1)
                .ToValueTask();

            source.Next(1);
            source.Next(2);

            Assert.Equal(2, await task);
        }

        [Fact]
        public async ValueTask FirstOrDefaultWithoutPredicate()
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
        public async ValueTask LastWithPredicate()
        {
            var source = new Source<int>();
            var task = source
                .Last(value => value < 2)
                .ToValueTask();

            source.Next(1);
            source.Next(2);
            source.Complete();

            Assert.Equal(1, await task);
        }

        [Fact]
        public async ValueTask LastWithoutPredicate()
        {
            var source = new Source<int>();
            var task = source
                .Last()
                .ToValueTask();

            source.Next(1);
            source.Next(2);
            source.Complete();

            Assert.Equal(2, await task);
        }

        [Fact]
        public async ValueTask LastOrDefaultWithPredicate()
        {
            var source = new Source<int>();
            var task = source
                .LastOrDefault(value => value < 2)
                .ToValueTask();

            source.Next(1);
            source.Next(2);
            source.Complete();

            Assert.Equal(1, await task);
        }

        [Fact]
        public async ValueTask LastOrDefaultWithoutPredicate()
        {
            var source = new Source<int>();
            var task = source
                .LastOrDefault()
                .ToValueTask();

            source.Next(1);
            source.Next(2);
            source.Complete();

            Assert.Equal(2, await task);
        }

        [Fact]
        public async ValueTask Max()
        {
            var source = new Source<int>();
            var task = source
                .Max()
                .ToValueTask();

            source.Next(1);
            source.Next(2);
            source.Complete();

            Assert.Equal(2, await task);
        }

        [Fact]
        public async ValueTask Min()
        {
            var source = new Source<int>();
            var task = source
                .Min()
                .ToValueTask();

            source.Next(1);
            source.Next(2);
            source.Complete();

            Assert.Equal(1, await task);
        }

        [Fact]
        public void Select()
        {
            var source = new Source<int>();
            var values = new List<int>();

            source
                .Select(value => value * 10)
                .Subscribe(value => values.Add(value));

            source.Next(1);
            source.Next(2);

            Assert.Equal(new[] { 10, 20 }, values);
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
            public void Complete() => OnCompleted();
        }
    }
}