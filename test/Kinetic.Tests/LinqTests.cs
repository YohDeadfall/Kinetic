using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Kinetic.Subjects;
using Xunit;

namespace Kinetic.Linq.Tests;

public class LinqTests
{
    [Fact]
    public async ValueTask All_WithPredicate()
    {
        var source = new PublishSubject<int>();
        var greaterThanTwo = source
            .Any(value => value > 2)
            .ToValueTask();
        var lessThanTwo = source
            .Any(value => value < 2)
            .ToValueTask();

        source.OnNext(1);
        source.OnNext(2);
        source.OnCompleted();

        Assert.False(await greaterThanTwo);
        Assert.True(await lessThanTwo);
    }

    [Fact]
    public async ValueTask All_WithoutPredicate()
    {
        var sourceFalse = new PublishSubject<bool>();
        var returnsFalse = sourceFalse.ToValueTask();

        sourceFalse.OnNext(false);
        sourceFalse.OnNext(true);
        sourceFalse.OnCompleted();

        Assert.False(await returnsFalse);

        var sourceTrue = new PublishSubject<bool>();
        var returnsTrue = sourceTrue.ToValueTask();

        sourceTrue.OnNext(true);
        sourceTrue.OnNext(true);
        sourceTrue.OnCompleted();

        Assert.True(await returnsTrue);
    }

    [Fact]
    public async ValueTask Any_WithPredicate()
    {
        var source = new PublishSubject<int>();
        var greaterThanOne = source
            .Any(value => value > 1)
            .ToValueTask();
        var lessThanOne = source
            .Any(value => value < 1)
            .ToValueTask();

        source.OnNext(1);
        source.OnNext(2);

        Assert.True(await greaterThanOne);
        Assert.False(await lessThanOne);
    }

    [Fact]
    public async ValueTask Any_WithoutPredicate()
    {
        var sourceEmpty = new PublishSubject<int>();
        var returnsFalse = sourceEmpty
            .Any()
            .ToValueTask();

        sourceEmpty.OnCompleted();

        Assert.False(await returnsFalse);

        var sourceNonEmpty = new PublishSubject<int>();
        var returnsTrue = sourceNonEmpty
            .Any()
            .ToValueTask();

        sourceNonEmpty.OnNext(1);

        Assert.True(await returnsTrue);
    }

    [Fact]
    public async ValueTask Contains_WithComparer()
    {
        var source = new PublishSubject<string>();
        var containsTwo = source
            .Contains("two", StringComparer.OrdinalIgnoreCase)
            .ToValueTask();
        var containsThree = source
            .Contains("three", StringComparer.OrdinalIgnoreCase)
            .ToValueTask();

        source.OnNext("One");
        source.OnNext("Two");

        Assert.True(await containsTwo);
        Assert.False(await containsThree);
    }

    [Fact]
    public async ValueTask Contains_WithoutComparer()
    {
        var source = new PublishSubject<string>();
        var containsTwo = source
            .Contains("Two")
            .ToValueTask();
        var containsThree = source
            .Contains("Three")
            .ToValueTask();

        source.OnNext("One");
        source.OnNext("Two");

        Assert.True(await containsTwo);
        Assert.False(await containsThree);
    }

    [Fact]
    public async ValueTask Count_WithPredicate()
    {
        var source = new PublishSubject<int>();
        var lessThanTwo = source
            .Count(value => value < 2)
            .ToValueTask();
        var greaterThanTwo = source
            .Count(value => value < 2)
            .ToValueTask();

        source.OnNext(1);
        source.OnNext(2);
        source.OnCompleted();

        Assert.Equal(1, await lessThanTwo);
        Assert.Equal(0, await greaterThanTwo);
    }

    [Fact]
    public async ValueTask Count_WithoutPredicate()
    {
        var source = new PublishSubject<int>();
        var count = source
            .ToValueTask();

        source.OnNext(1);
        source.OnNext(2);
        source.OnCompleted();

        Assert.Equal(3, await count);
    }

    [Fact]
    public async ValueTask Distinct()
    {
        var source = new PublishSubject<int>();
        var values = source
            .Distinct()
            .ToArray()
            .ToValueTask();

        source.OnNext(1);
        source.OnNext(1);
        source.OnNext(2);
        source.OnNext(2);

        Assert.Equal(new[] { 1, 2 }, await values);
    }

    [Fact]
    public async ValueTask First_WithPredicate()
    {
        var source = new PublishSubject<int>();
        var value = source
            .First(value => value > 1)
            .ToValueTask();

        source.OnNext(1);
        source.OnNext(2);

        Assert.Equal(2, await value);
    }

    [Fact]
    public async ValueTask First_WithoutPredicate()
    {
        var source = new PublishSubject<int>();
        var value = source
            .First()
            .ToValueTask();

        source.OnNext(1);
        source.OnNext(2);

        Assert.Equal(1, await value);
    }

    [Fact]
    public async ValueTask FirstOrDefault_WithPredicate()
    {
        var source = new PublishSubject<int>();
        var value = source
            .FirstOrDefault(value => value > 1)
            .ToValueTask();

        source.OnNext(1);
        source.OnNext(2);

        Assert.Equal(2, await value);
    }

    [Fact]
    public async ValueTask FirstOrDefault_WithoutPredicate()
    {
        var source = new PublishSubject<int>();
        var value = source
            .FirstOrDefault()
            .ToValueTask();

        source.OnNext(1);
        source.OnNext(2);

        Assert.Equal(1, await value);
    }

    [Fact]
    public async ValueTask Last_WithPredicate()
    {
        var source = new PublishSubject<int>();
        var value = source
            .Last(value => value < 2)
            .ToValueTask();

        source.OnNext(1);
        source.OnNext(2);
        source.OnCompleted();

        Assert.Equal(1, await value);
    }

    [Fact]
    public async ValueTask Last_WithoutPredicate()
    {
        var source = new PublishSubject<int>();
        var value = source
            .Last()
            .ToValueTask();

        source.OnNext(1);
        source.OnNext(2);
        source.OnCompleted();

        Assert.Equal(2, await value);
    }

    [Fact]
    public async ValueTask LastOrDefault_WithPredicate()
    {
        var source = new PublishSubject<int>();
        var value = source
            .LastOrDefault(value => value < 2)
            .ToValueTask();

        source.OnNext(1);
        source.OnNext(2);
        source.OnCompleted();

        Assert.Equal(1, await value);
    }

    [Fact]
    public async ValueTask LastOrDefault_WithoutPredicate()
    {
        var source = new PublishSubject<int>();
        var value = source
            .LastOrDefault()
            .ToValueTask();

        source.OnNext(1);
        source.OnNext(2);
        source.OnCompleted();

        Assert.Equal(2, await value);
    }

    [Fact]
    public async ValueTask Max()
    {
        var source = new PublishSubject<int>();
        var value = source
            .Max()
            .ToValueTask();

        source.OnNext(1);
        source.OnNext(2);
        source.OnCompleted();

        Assert.Equal(2, await value);
    }

    [Fact]
    public async ValueTask Min()
    {
        var source = new PublishSubject<int>();
        var value = source
            .Min()
            .ToValueTask();

        source.OnNext(1);
        source.OnNext(2);
        source.OnCompleted();

        Assert.Equal(1, await value);
    }

    [Fact]
    public async ValueTask Select()
    {
        var source = new PublishSubject<int>();
        var values = source
            .Select(value => value * 10)
            .ToArray()
            .ToValueTask();

        source.OnNext(1);
        source.OnNext(2);

        Assert.Equal(new[] { 10, 20 }, await values);
    }

    [Fact]
    public async ValueTask Single_WithPredicate()
    {
        var source = new PublishSubject<int>();
        var value = source
            .Single(value => value > 1)
            .ToValueTask();

        source.OnNext(1);
        source.OnNext(2);
        source.OnCompleted();

        Assert.Equal(2, await value);
    }

    [Fact]
    public async ValueTask Single_WithoutPredicate()
    {
        var source = new PublishSubject<int>();
        var value = source
            .Single()
            .ToValueTask();

        source.OnNext(1);
        source.OnCompleted();

        Assert.Equal(1, await value);
    }

    [Fact]
    public async ValueTask SingleOrDefault_WithPredicate()
    {
        var source = new PublishSubject<int>();
        var value = source
            .SingleOrDefault(value => value > 1)
            .ToValueTask();

        source.OnNext(1);
        source.OnNext(2);
        source.OnCompleted();

        Assert.Equal(2, await value);
    }

    [Fact]
    public async ValueTask SingleOrDefault_WithoutPredicate()
    {
        var source = new PublishSubject<int>();
        var value = source
            .SingleOrDefault()
            .ToValueTask();

        source.OnNext(1);
        source.OnCompleted();

        Assert.Equal(1, await value);
    }

    [Fact]
    public async ValueTask Skip()
    {
        var source = new PublishSubject<int>();
        var values = source
            .Skip(2)
            .ToArray()
            .ToValueTask();

        source.OnNext(0);
        source.OnNext(1);
        source.OnNext(2);
        source.OnNext(3);

        Assert.Equal(new[] { 2, 3 }, await values);
    }

    [Fact]
    public async ValueTask SkipWhile()
    {
        var source = new PublishSubject<int>();
        var values = source
            .SkipWhile(value => value < 2)
            .ToArray()
            .ToValueTask();

        source.OnNext(1);
        source.OnNext(2);
        source.OnNext(3);

        Assert.Equal(new[] { 2, 3 }, await values);
    }

    [Fact]
    public async ValueTask Take()
    {
        var source = new PublishSubject<int>();
        var values = source
            .Take(2)
            .ToArray()
            .ToValueTask();

        source.OnNext(1);
        source.OnNext(2);
        source.OnNext(3);

        Assert.Equal(new[] { 1, 2 }, await values);
    }

    [Fact]
    public async ValueTask TakeWhile()
    {
        var source = new PublishSubject<int>();
        var values = source
            .TakeWhile(value => value < 2)
            .ToArray()
            .ToValueTask();

        source.OnNext(0);
        source.OnNext(1);
        source.OnNext(2);
        source.OnNext(3);

        Assert.Equal(new[] { 0, 1 }, await values);
    }

    [Fact]
    public async ValueTask Then()
    {
        var container = new Container<int>();
        var values = container.PublishSubject.Changed
            .Then(value => value)
            .ToArray()
            .ToValueTask();

        var source = container.PublishSubject.Get();

        source.OnNext(1);
        source.OnNext(2);

        var sourceNew = new PublishSubject<int>();

        container.PublishSubject.Set(sourceNew);
        sourceNew.OnNext(10);
        sourceNew.OnNext(20);

        source.OnNext(3);
        source.OnNext(4);

        Assert.Equal(new[] { 1, 2, 10, 20 }, await values);
    }

    [Fact]
    public async ValueTask ToArray()
    {
        var source = new PublishSubject<int>();
        var value = source
            .ToArray()
            .ToValueTask();

        source.OnNext(1);
        source.OnNext(2);
        source.OnNext(3);

        Assert.Equal(
            expected: new[] { 1, 2, 3 },
            actual: await value);
    }

    [Fact]
    public async ValueTask ToDictionary()
    {
        var source = new PublishSubject<(string text, int number)>();
        var taskWithoutComparer = source
            .ToDictionary(value => value.text, value => value.number * 3)
            .ToValueTask();
        var taskWithComparer = source
            .ToDictionary(value => value.text, value => value.number * 3, StringComparer.OrdinalIgnoreCase)
            .ToValueTask();

        source.OnNext(("One", 1));
        source.OnNext(("Two", 2));

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
    public async ValueTask ToList()
    {
        var source = new PublishSubject<int>();
        var value = source
            .ToList()
            .ToValueTask();

        source.OnNext(1);
        source.OnNext(2);
        source.OnNext(3);

        Assert.Equal(new[] { 1, 2, 3 }, await value);
    }

    [Fact]
    public async ValueTask ToObservable()
    {
        var source = new PublishSubject<int>();
        var observable = source
            .Select(value => value)
            .ToObservable();

        var values = observable
            .ToArray()
            .ToValueTask();

        source.OnNext(1);
        source.OnNext(2);

        Assert.Equal(new[] { 1, 2 }, await values);
    }

    [Fact]
    public async ValueTask Where()
    {
        var source = new PublishSubject<int>();
        var values = source
            .Where(value => value > 2)
            .ToArray()
            .ToValueTask();

        source.OnNext(1);
        source.OnNext(3);
        source.OnNext(2);
        source.OnNext(4);

        Assert.Equal(new[] { 3, 4 }, await values);
    }

    private sealed class Container<T> : ObservableObject
    {
        private PublishSubject<T> _source;

        public Container() => _source = new PublishSubject<T>();
        public Property<PublishSubject<T>> PublishSubject => Property(ref _source);
    }
}