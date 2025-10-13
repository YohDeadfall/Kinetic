using System;
using System.Collections.Generic;
using System.Threading;
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
    public async ValueTask FromAsync()
    {
        var cts = new TaskCompletionSource();
        var source = Observable
            .FromAsync(async _ => await cts.Task)
            .ToValueTask();

        cts.SetResult();

        Assert.Equal(default, await source);
    }

    [Fact]
    public async ValueTask FromAsync_CancelsOnDispose()
    {
        var cts = new TaskCompletionSource<int?>();
        var source = Observable
            .FromAsync(async ct =>
            {
                ct.ThrowIfCancellationRequested();
                return await cts.Task;
            })
            .FirstOrDefault()
            .ToValueTask();

        cts.SetResult(42);

        Assert.Null(await source);
    }

    [Fact]
    public async ValueTask FromAsyncGeneric()
    {
        var cts = new TaskCompletionSource<int>();
        var source = Observable
            .FromAsync(async _ => await cts.Task)
            .ToValueTask();

        cts.SetResult(42);

        Assert.Equal(42, await source);
    }

    [Fact]
    public async ValueTask FromAsyncGeneric_CancelsOnDispose()
    {
        var cts = new TaskCompletionSource<int?>();
        var source = Observable
            .FromAsync(async ct =>
            {
                ct.ThrowIfCancellationRequested();
                return await cts.Task;
            })
            .FirstOrDefault()
            .ToValueTask();

        cts.SetResult(42);

        Assert.Null(await source);
    }

    [Fact]
    public async ValueTask FromGenerator()
    {
        var source = Observable
            .Create<int>(observer =>
            {
                observer.OnNext(42);
                observer.OnCompleted();
                return Disposable.Empty;
            })
            .ToValueTask();

        Assert.Equal(42, await source);
    }

    [Fact]
    public async ValueTask FromRange()
    {
        Assert.Equal([0, 1, 2], await Observable.FromRange(0, 3).ToArray());
        Assert.Equal([0, 1, 2, 3], await Observable.FromRangeInclusive(0, 3).ToArray());
        Assert.Equal([0, 2], await Observable.FromRange(0, 4, step: 2).ToArray());
        Assert.Equal([0, 2, 4], await Observable.FromRange(0, 4, step: 2).ToArray());
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
    public async ValueTask SelectAsync_WithTask()
    {
        var source = new PublishSubject<Task<int>>();
        var values = source
            .SelectAwait(value => value)
            .ToArray()
            .ToValueTask();

        source.OnNext(Task.FromResult(1));
        source.OnNext(TaskWithYeldedResult(2));

        Assert.Equal(new[] { 1, 2 }, await values);
    }

    [Fact]
    public async ValueTask SelectAsync_WithValueTask()
    {
        var source = new PublishSubject<ValueTask<int>>();
        var values = source
            .SelectAwait(value => value)
            .ToArray()
            .ToValueTask();

        source.OnNext(ValueTask.FromResult(1));
        source.OnNext(ValueTaskWithYeldedResult(2));

        Assert.Equal(new[] { 1, 2 }, await values);
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
    public async ValueTask Switch()
    {
        var outer = new PublishSubject<Subject<string>>();
        var innerA = new PublishSubject<string>();
        var innerB = new PublishSubject<string>();

        var values = outer
            .Switch()
            .ToArray()
            .ToValueTask();

        innerA.OnNext("initial");
        outer.OnNext(innerA);
        innerA.OnNext("visible from A");
        outer.OnNext(innerB);
        innerA.OnNext("invisible from A");
        innerB.OnNext("visible from B");

        Assert.Equal(
            new[] { "visible from A", "visible from B" },
            await values);
    }

    [Fact]
    public void SwitchCompletesAfterInner()
    {
        var outer = new PublishSubject<Subject<string>>();
        var inner = new PublishSubject<string>();
        var result = outer.Switch().ToValueTask();

        outer.OnNext(inner);
        inner.OnCompleted();

        Assert.False(result.IsCompleted);

        outer.OnCompleted();

        Assert.True(result.IsCompleted);
    }

    [Fact]
    public void SwitchCompletesNotBeforeInner()
    {
        var outer = new PublishSubject<Subject<string>>();
        var inner = new PublishSubject<string>();
        var result = outer.Switch().ToValueTask();

        outer.OnNext(inner);
        outer.OnCompleted();

        Assert.False(result.IsCompleted);

        inner.OnCompleted();

        Assert.True(result.IsCompleted);
    }

    [Fact]
    public void SwitchCompletesIfNoInner()
    {
        var outer = new PublishSubject<Subject<string>?>();
        var inner = new PublishSubject<string>();
        var result = outer.Switch().ToValueTask();

        outer.OnNext(inner);
        outer.OnNext(null);
        outer.OnCompleted();

        Assert.True(result.IsCompleted);
    }

    [Fact]
    public async ValueTask Throttle()
    {
        var delay = TimeSpan.FromMilliseconds(1);
        var context = SynchronizationContext.Current;

        try
        {
            SynchronizationContext.SetSynchronizationContext(
                new WithoutSynchronizationContext());

            await ThrottleCore(false).ConfigureAwait(false);

            SynchronizationContext.SetSynchronizationContext(
                new WithSynchronizationContext { Delay = delay });

            await ThrottleCore(true).ConfigureAwait(false);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(context);
        }

        async ValueTask ThrottleCore(bool continueOnCapturedContext)
        {
            var source = new PublishSubject<int>();
            var values = source
                .Throttle(delay, continueOnCapturedContext)
                .ToArray()
                .ToValueTask();

            source.OnNext(1);
            source.OnNext(2);

            // To avoid a race between Task.Delay
            // and Throttle double the delay here.
            await Task.Delay(delay * 2).ConfigureAwait(false);

            source.OnNext(10);
            source.OnNext(20);

            Assert.Equal(new[] { 2, 20 }, await values.ConfigureAwait(false));
        }
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

    [Fact]
    public async ValueTask WhereAsync_WithTask()
    {
        var source = new PublishSubject<int>();
        var values = source
            .WhereAwait(value =>
                value > 2 is var result &&
                value % 2 == 0
                ? Task.FromResult(result)
                : TaskWithYeldedResult(result))
            .ToArray()
            .ToValueTask();

        source.OnNext(1);
        source.OnNext(3);
        source.OnNext(2);
        source.OnNext(4);

        Assert.Equal(new[] { 3, 4 }, await values);
    }

    [Fact]
    public async ValueTask WhereAsync_WithValueTask()
    {
        var source = new PublishSubject<int>();
        var values = source
            .WhereAwait(value =>
                value > 2 is var result &&
                value % 2 == 0
                ? ValueTask.FromResult(result)
                : ValueTaskWithYeldedResult(result))
            .ToArray()
            .ToValueTask();

        source.OnNext(1);
        source.OnNext(3);
        source.OnNext(2);
        source.OnNext(4);

        Assert.Equal(new[] { 3, 4 }, await values);
    }

    private static async Task<T> TaskWithYeldedResult<T>(T value)
    {
        await Task.Yield();
        return value;
    }

    private static async ValueTask<T> ValueTaskWithYeldedResult<T>(T value)
    {
        await Task.Yield();
        return value;
    }

    private sealed class WithSynchronizationContext : SynchronizationContext
    {
        public TimeSpan Delay { get; set; }

        public override void Post(SendOrPostCallback d, object? state) =>
            Task.Delay(Delay).ContinueWith(_ => d.Invoke(state));

        public override void Send(SendOrPostCallback d, object? state) =>
            throw new InvalidOperationException("Must not be used.");
    }


    private sealed class WithoutSynchronizationContext : SynchronizationContext
    {
        public override void Post(SendOrPostCallback d, object? state) =>
            throw new InvalidOperationException("Must not be used.");

        public override void Send(SendOrPostCallback d, object? state) =>
            throw new InvalidOperationException("Must not be used.");
    }
}