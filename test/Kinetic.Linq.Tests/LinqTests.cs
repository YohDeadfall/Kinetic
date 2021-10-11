using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Kinetic.Linq.Tests
{
    public class LinqTests
    {
        [Fact]
        public async ValueTask All_WithPredicate()
        {
            var source = new Source<int>();
            var greaterThanTwo = source
                .Any(value => value > 2)
                .ToValueTask();
            var lessThanTwo = source
                .Any(value => value < 2)
                .ToValueTask();

            source.Next(1);
            source.Next(2);
            source.Complete();

            Assert.False(await greaterThanTwo);
            Assert.True(await lessThanTwo);
        }

        [Fact]
        public async ValueTask All_WithoutPredicate()
        {
            var sourceFalse = new Source<bool>();
            var returnsFalse = sourceFalse.ToValueTask();

            sourceFalse.Next(false);
            sourceFalse.Next(true);
            sourceFalse.Complete();

            Assert.False(await returnsFalse);

            var sourceTrue = new Source<bool>();
            var returnsTrue = sourceTrue.ToValueTask();

            sourceTrue.Next(true);
            sourceTrue.Next(true);
            sourceTrue.Complete();

            Assert.True(await returnsTrue);
        }

        [Fact]
        public async ValueTask Any_WithPredicate()
        {
            var source = new Source<int>();
            var greaterThanOne = source
                .Any(value => value > 1)
                .ToValueTask();
            var lessThanOne = source
                .Any(value => value < 1)
                .ToValueTask();

            source.Next(1);
            source.Next(2);

            Assert.True(await greaterThanOne);
            Assert.False(await lessThanOne);
        }

        [Fact]
        public async ValueTask Any_WithoutPredicate()
        {
            var sourceEmpty = new Source<int>();
            var returnsFalse = sourceEmpty
                .Any()
                .ToValueTask();

            sourceEmpty.Complete();

            Assert.False(await returnsFalse);

            var sourceNonEmpty = new Source<int>();
            var returnsTrue = sourceNonEmpty
                .Any()
                .ToValueTask();

            sourceNonEmpty.Next(1);

            Assert.True(await returnsTrue);
        }

        [Fact]
        public async ValueTask Contains_WithComparer()
        {
            var source = new Source<string>();
            var containsTwo = source
                .Contains("two", StringComparer.OrdinalIgnoreCase)
                .ToValueTask();
            var containsThree = source
                .Contains("three", StringComparer.OrdinalIgnoreCase)
                .ToValueTask();

            source.Next("One");
            source.Next("Two");

            Assert.True(await containsTwo);
            Assert.False(await containsThree);
        }

        [Fact]
        public async ValueTask Contains_WithoutComparer()
        {
            var source = new Source<string>();
            var containsTwo = source
                .Contains("Two")
                .ToValueTask();
            var containsThree = source
                .Contains("Three")
                .ToValueTask();

            source.Next("One");
            source.Next("Two");

            Assert.True(await containsTwo);
            Assert.False(await containsThree);
        }

        [Fact]
        public async ValueTask Count_WithPredicate()
        {
            var source = new Source<int>();
            var lessThanTwo = source
                .Count(value => value < 2)
                .ToValueTask();
            var greaterThanTwo = source
                .Count(value => value < 2)
                .ToValueTask();

            source.Next(1);
            source.Next(2);
            source.Complete();

            Assert.Equal(1, await lessThanTwo);
            Assert.Equal(0, await greaterThanTwo);
        }

        [Fact]
        public async ValueTask Count_WithoutPredicate()
        {
            var source = new Source<int>();
            var task = source
                .ToValueTask();

            source.Next(1);
            source.Next(2);
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
        public async ValueTask First_WithPredicate()
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
        public async ValueTask First_WithoutPredicate()
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
        public async ValueTask FirstOrDefault_WithPredicate()
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
        public async ValueTask FirstOrDefault_WithoutPredicate()
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
        public async ValueTask Last_WithPredicate()
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
        public async ValueTask Last_WithoutPredicate()
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
        public async ValueTask LastOrDefault_WithPredicate()
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
        public async ValueTask LastOrDefault_WithoutPredicate()
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
        public async ValueTask Single_WithPredicate()
        {
            var source = new Source<int>();
            var task = source
                .Single(value => value > 1)
                .ToValueTask();

            source.Next(1);
            source.Next(2);
            source.Complete();

            Assert.Equal(2, await task);
        }

        [Fact]
        public async ValueTask Single_WithoutPredicate()
        {
            var source = new Source<int>();
            var task = source
                .Single()
                .ToValueTask();

            source.Next(1);
            source.Complete();

            Assert.Equal(1, await task);
        }

        [Fact]
        public async ValueTask SingleOrDefault_WithPredicate()
        {
            var source = new Source<int>();
            var task = source
                .SingleOrDefault(value => value > 1)
                .ToValueTask();

            source.Next(1);
            source.Next(2);
            source.Complete();

            Assert.Equal(2, await task);
        }

        [Fact]
        public async ValueTask SingleOrDefault_WithoutPredicate()
        {
            var source = new Source<int>();
            var task = source
                .SingleOrDefault()
                .ToValueTask();

            source.Next(1);
            source.Complete();

            Assert.Equal(1, await task);
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
        public void Then()
        {
            var container = new Container<int>();
            var values = new List<int>();

            container.Source.Changed
                .Then(value => value)
                .Subscribe(value => values.Add(value));

            var source = container.Source.Get();

            source.Next(1);
            source.Next(2);

            var sourceNew = new Source<int>();

            container.Source.Set(sourceNew);
            sourceNew.Next(10);
            sourceNew.Next(20);

            source.Next(3);
            source.Next(4);

            Assert.Equal(new[] { 1, 2, 10, 20 }, values);
        }

        [Fact]
        public async ValueTask ToDictionary()
        {
            var source = new Source<(string text, int number)>();
            var taskWithoutComparer = source
                .ToDictionary(value => value.text, value => value.number * 3)
                .ToValueTask();
            var taskWithComparer = source
                .ToDictionary(value => value.text, value => value.number * 3, StringComparer.OrdinalIgnoreCase)
                .ToValueTask();

            source.Next(("One", 1));
            source.Next(("Two", 2));

            var dictionaryWithoutComparer = await taskWithoutComparer;
            var dictionaryWithComparer = await taskWithComparer;
            var expected = new[]
            {
                KeyValuePair.Create("One", 3),
                KeyValuePair.Create("Two", 6)
            };

            Assert.Equal(dictionaryWithoutComparer, expected);
            Assert.Equal(dictionaryWithoutComparer.Comparer, EqualityComparer<string>.Default);

            Assert.Equal(dictionaryWithComparer, expected);
            Assert.Equal(dictionaryWithoutComparer.Comparer, StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public void ToObservable()
        {
            var source = new Source<int>();
            var values = new List<int>();
            var observable = source
                .Select(value => value)
                .ToObservable();

            observable.Subscribe(value => values.Add(value));

            source.Next(1);
            source.Next(2);

            Assert.Equal(new[] { 1, 2 }, values);
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

        private sealed class Container<T> : ObservableObject
        {
            private Source<T> _source;

            public Container() => _source = new Source<T>();
            public Property<Source<T>> Source => Property(ref _source);
        }
    }
}