using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Kinetic.Linq;

public static partial class ObservableExtensions
{
    public static Operator<Aggregate<Subscribe<TSource>, TSource>, TSource> Aggregate<TSource>(
        this IObservable<TSource> source, Func<TSource?, TSource, TSource> accumulator)
    {
        return source.Subscribe().Aggregate(accumulator);
    }

    public static Operator<AggregateWithSeed<Subscribe<TSource>, TSource, TAccumulate>, TAccumulate> Aggregate<TSource, TAccumulate>(
        this IObservable<TSource> source, TAccumulate seed, Func<TAccumulate, TSource, TAccumulate> accumulator)
    {
        return source.Subscribe().Aggregate(seed, accumulator);
    }

    public static Operator<Select<AggregateWithSeed<Subscribe<TSource>, TSource, TAccumulate>, TAccumulate, TResult>, TResult> Aggregate<TSource, TAccumulate, TResult>(
        this IObservable<TSource> source, TAccumulate seed, Func<TAccumulate?, TSource, TAccumulate> accumulator, Func<TAccumulate, TResult> selector)
    {
        return source.Subscribe().Aggregate(seed, accumulator, selector);
    }

    public static Operator<All<Subscribe<bool>>, bool> All(
        this IObservable<bool> source)
    {
        return source.Subscribe().All();
    }

    public static Operator<All<Select<Subscribe<TSource>, TSource, bool>>, bool> All<TSource>(
        this IObservable<TSource> source, Func<TSource, bool> predicate)
    {
        return source.Subscribe().All(predicate);
    }

    public static Operator<Any<Subscribe<TSource>, TSource>, bool> Any<TSource>(
        this IObservable<TSource> source)
    {
        return source.Subscribe().Any();
    }

    public static Operator<Any<Where<Subscribe<TSource>, TSource>, TSource>, bool> Any<TSource>(
        this IObservable<TSource> source, Func<TSource, bool> predicate)
    {
        return source.Subscribe().Any(predicate);
    }

    public static Operator<Contains<Subscribe<TSource>, TSource>, bool> Contains<TSource>(
        this IObservable<TSource> source, TSource value)
    {
        return source.Subscribe().Contains(value);
    }

    public static Operator<Contains<Subscribe<TSource>, TSource>, bool> Contains<TSource>(
        this IObservable<TSource> source, TSource value, IEqualityComparer<TSource>? comparer)
    {
        return source.Subscribe().Contains(value, comparer);
    }

    public static Operator<Count<Subscribe<TSource>, TSource>, int> Count<TSource>(
        this IObservable<TSource> source)
    {
        return source.Subscribe().Count();
    }

    public static Operator<Count<Where<Subscribe<TSource>, TSource>, TSource>, int> Count<TSource>(
        this IObservable<TSource> source, Func<TSource, bool> predicate)
    {
        return source.Subscribe().Count(predicate);
    }

    public static Operator<Distinct<Subscribe<TSource>, TSource>, TSource> Distinct<TSource>(
        this IObservable<TSource> source)
    {
        return source.Subscribe().Distinct();
    }

    public static Operator<Distinct<Subscribe<TSource>, TSource>, TSource> Distinct<TSource>(
        this IObservable<TSource> source, IEqualityComparer<TSource>? comparer)
    {
        return source.Subscribe().Distinct(comparer);
    }

    public static Operator<DistinctBy<Subscribe<TSource>, TSource, TKey>, TSource> DistinctBy<TSource, TKey>(
        this IObservable<TSource> source, Func<TSource, TKey> keySelector)
    {
        return source.Subscribe().DistinctBy(keySelector);
    }

    public static Operator<DistinctBy<Subscribe<TSource>, TSource, TKey>, TSource> DistinctBy<TSource, TKey>(
        this IObservable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TSource>? comparer)
    {
        return source.Subscribe().DistinctBy(keySelector, comparer);
    }

    public static Operator<First<Subscribe<TSource>, TSource>, TSource> First<TSource>(
        this IObservable<TSource> source)
    {
        return source.Subscribe().First();
    }

    public static Operator<First<Where<Subscribe<TSource>, TSource>, TSource>, TSource> First<TSource>(
        this IObservable<TSource> source, Func<TSource, bool> predicate)
    {
        return source.Subscribe().First(predicate);
    }

    public static Operator<FirstOrDefault<Cast<Subscribe<TSource>, TSource, TSource?>, TSource?>, TSource?> FirstOrDefault<TSource>(
        this IObservable<TSource> source)
    {
        return source.Subscribe().FirstOrDefault();
    }

    public static Operator<FirstOrDefault<Subscribe<TSource>, TSource>, TSource> FirstOrDefault<TSource>(
        this IObservable<TSource> source, TSource defaultValue)
    {
        return source.Subscribe().FirstOrDefault(defaultValue);
    }

    public static Operator<FirstOrDefault<Cast<Where<Subscribe<TSource>, TSource>, TSource, TSource?>, TSource?>, TSource?> FirstOrDefault<TSource>(
        this IObservable<TSource> source, Func<TSource, bool> predicate)
    {
        return source.Subscribe().FirstOrDefault(predicate);
    }

    public static Operator<FirstOrDefault<Where<Subscribe<TSource>, TSource>, TSource>, TSource> FirstOrDefault<TSource>(
        this IObservable<TSource> source, Func<TSource, bool> predicate, TSource defaultValue)
    {
        return source.Subscribe().FirstOrDefault(predicate, defaultValue);
    }

    public static Operator<Last<Subscribe<TSource>, TSource>, TSource> Last<TSource>(
        this IObservable<TSource> source)
    {
        return source.Subscribe().Last();
    }

    public static Operator<Last<Where<Subscribe<TSource>, TSource>, TSource>, TSource> Last<TSource>(
        this IObservable<TSource> source, Func<TSource, bool> predicate)
    {
        return source.Subscribe().Last(predicate);
    }

    public static Operator<LastOrDefault<Cast<Subscribe<TSource>, TSource, TSource?>, TSource?>, TSource?> LastOrDefault<TSource>(
        this IObservable<TSource> source)
    {
        return source.Subscribe().LastOrDefault();
    }

    public static Operator<LastOrDefault<Subscribe<TSource>, TSource>, TSource> LastOrDefault<TSource>(
        this IObservable<TSource> source, TSource defaultValue)
    {
        return source.Subscribe().LastOrDefault(defaultValue);
    }

    public static Operator<LastOrDefault<Cast<Where<Subscribe<TSource>, TSource>, TSource, TSource?>, TSource?>, TSource?> LastOrDefault<TSource>(
        this IObservable<TSource> source, Func<TSource, bool> predicate)
    {
        return source.Subscribe().LastOrDefault(predicate);
    }

    public static Operator<LastOrDefault<Where<Subscribe<TSource>, TSource>, TSource>, TSource> LastOrDefault<TSource>(
        this IObservable<TSource> source, Func<TSource, bool> predicate, TSource defaultValue)
    {
        return source.Subscribe().LastOrDefault(predicate, defaultValue);
    }

    public static Operator<Single<Subscribe<TSource>, TSource>, TSource> Single<TSource>(
        this IObservable<TSource> source)
    {
        return source.Subscribe().Single();
    }

    public static Operator<Single<Where<Subscribe<TSource>, TSource>, TSource>, TSource> Single<TSource>(
        this IObservable<TSource> source, Func<TSource, bool> predicate)
    {
        return source.Subscribe().Single(predicate);
    }

    public static Operator<SingleOrDefault<Cast<Subscribe<TSource>, TSource, TSource?>, TSource?>, TSource?> SingleOrDefault<TSource>(
        this IObservable<TSource> source)
    {
        return source.Subscribe().SingleOrDefault();
    }

    public static Operator<SingleOrDefault<Subscribe<TSource>, TSource>, TSource> SingleOrDefault<TSource>(
        this IObservable<TSource> source, TSource defaultValue)
    {
        return source.Subscribe().SingleOrDefault(defaultValue);
    }

    public static Operator<SingleOrDefault<Cast<Where<Subscribe<TSource>, TSource>, TSource, TSource?>, TSource?>, TSource?> SingleOrDefault<TSource>(
        this IObservable<TSource> source, Func<TSource, bool> predicate)
    {
        return source.Subscribe().SingleOrDefault(predicate);
    }

    public static Operator<SingleOrDefault<Where<Subscribe<TSource>, TSource>, TSource>, TSource> SingleOrDefault<TSource>(
        this IObservable<TSource> source, Func<TSource, bool> predicate, TSource defaultValue)
    {
        return source.Subscribe().SingleOrDefault(predicate, defaultValue);
    }

    public static Operator<Max<Subscribe<TSource>, TSource>, TSource> Max<TSource>(
        this IObservable<TSource> source)
    {
        return source.Subscribe().Max();
    }

    public static Operator<Max<Subscribe<TSource>, TSource>, TSource> Max<TSource>(
        this IObservable<TSource> source, IComparer<TSource>? comparer)
    {
        return source.Subscribe().Max(comparer);
    }

    public static Operator<MaxBy<Subscribe<TSource>, TSource, TKey>, TSource> MaxBy<TSource, TKey>(
        this IObservable<TSource> source, Func<TSource, TKey> keySelector)
    {
        return source.Subscribe().MaxBy(keySelector);
    }

    public static Operator<MaxBy<Subscribe<TSource>, TSource, TKey>, TSource> MaxBy<TSource, TKey>(
        this IObservable<TSource> source, Func<TSource, TKey> keySelector, IComparer<TKey>? comparer)
    {
        return source.Subscribe().MaxBy(keySelector, comparer);
    }

    public static Operator<Min<Subscribe<TSource>, TSource>, TSource> Min<TSource>(
        this IObservable<TSource> source)
    {
        return source.Subscribe().Min();
    }

    public static Operator<Min<Subscribe<TSource>, TSource>, TSource> Min<TSource>(
        this IObservable<TSource> source, IComparer<TSource>? comparer)
    {
        return source.Subscribe().Min(comparer);
    }

    public static Operator<MinBy<Subscribe<TSource>, TSource, TKey>, TSource> MinBy<TSource, TKey>(
        this IObservable<TSource> source, Func<TSource, TKey> keySelector)
    {
        return source.Subscribe().MinBy(keySelector);
    }

    public static Operator<MinBy<Subscribe<TSource>, TSource, TKey>, TSource> MinBy<TSource, TKey>(
        this IObservable<TSource> source, Func<TSource, TKey> keySelector, IComparer<TKey>? comparer)
    {
        return source.Subscribe().MinBy(keySelector, comparer);
    }

    public static Operator<Select<Subscribe<TSource>, TSource, TResult>, TResult> Select<TSource, TResult>(
        this IObservable<TSource> source, Func<TSource, TResult> selector)
    {
        return source.Subscribe().Select(selector);
    }

    public static Operator<SelectIndexed<Subscribe<TSource>, TSource, TResult>, TResult> Select<TSource, TResult>(
        this IObservable<TSource> source, Func<TSource, int, TResult> selector)
    {
        return source.Subscribe().Select(selector);
    }

    public static Operator<SelectAwait<Subscribe<TSource>, TSource, TResult>, TResult> SelectAwait<TSource, TResult>(
        this IObservable<TSource> source, Func<TSource, ValueTask<TResult>> selector)
    {
        return source.Subscribe().SelectAwait(selector);
    }

    public static Operator<SelectAwaitIndexed<Subscribe<TSource>, TSource, TResult>, TResult> SelectAwait<TSource, TResult>(
        this IObservable<TSource> source, Func<TSource, int, ValueTask<TResult>> selector)
    {
        return source.Subscribe().SelectAwait(selector);
    }

    public static Operator<SelectAwaitTask<Subscribe<TSource>, TSource, TResult>, TResult> SelectAwait<TSource, TResult>(
        this IObservable<TSource> source, Func<TSource, Task<TResult>> selector)
    {
        return source.Subscribe().SelectAwait(selector);
    }

    public static Operator<SelectAwaitTaskIndexed<Subscribe<TSource>, TSource, TResult>, TResult> SelectAwait<TSource, TResult>(
        this IObservable<TSource> source, Func<TSource, int, Task<TResult>> selector)
    {
        return source.Subscribe().SelectAwait(selector);
    }

    public static Operator<Where<Subscribe<TSource>, TSource>, TSource> Where<TSource>(
        this IObservable<TSource> source, Func<TSource, bool> predicate)
    {
        return source.Subscribe().Where(predicate);
    }

    public static Operator<WhereIndexed<Subscribe<TSource>, TSource>, TSource> Where<TSource>(
        this IObservable<TSource> source, Func<TSource, int, bool> predicate)
    {
        return source.Subscribe().Where(predicate);
    }

    public static Operator<WhereAwait<Subscribe<TSource>, TSource>, TSource> WhereAwait<TSource>(
        this IObservable<TSource> source, Func<TSource, ValueTask<bool>> predicate)
    {
        return source.Subscribe().WhereAwait(predicate);
    }

    public static Operator<WhereAwaitIndexed<Subscribe<TSource>, TSource>, TSource> WhereAwait<TSource>(
        this IObservable<TSource> source, Func<TSource, int, ValueTask<bool>> predicate)
    {
        return source.Subscribe().WhereAwait(predicate);
    }

    public static Operator<WhereAwaitTask<Subscribe<TSource>, TSource>, TSource> WhereAwait<TSource>(
        this IObservable<TSource> source, Func<TSource, Task<bool>> predicate)
    {
        return source.Subscribe().WhereAwait(predicate);
    }

    public static Operator<WhereAwaitTaskIndexed<Subscribe<TSource>, TSource>, TSource> WhereAwait<TSource>(
        this IObservable<TSource> source, Func<TSource, int, Task<bool>> predicate)
    {
        return source.Subscribe().WhereAwait(predicate);
    }

    public static Operator<Skip<Subscribe<TSource>, TSource>, TSource> Skip<TSource>(
        this IObservable<TSource> source, int count)
    {
        return source.Subscribe().Skip(count);
    }

    public static Operator<SkipWhile<Subscribe<TSource>, TSource>, TSource> SkipWhile<TSource>(
        this IObservable<TSource> source, Func<TSource, bool> predicate)
    {
        return source.Subscribe().SkipWhile(predicate);
    }

    public static Operator<SkipWhileIndexed<Subscribe<TSource>, TSource>, TSource> SkipWhile<TSource>(
        this IObservable<TSource> source, Func<TSource, int, bool> predicate)
    {
        return source.Subscribe().SkipWhile(predicate);
    }

    public static Operator<Take<Subscribe<TSource>, TSource>, TSource> Take<TSource>(
        this IObservable<TSource> source, int count)
    {
        return source.Subscribe().Take(count);
    }

    public static Operator<TakeWhile<Subscribe<TSource>, TSource>, TSource> TakeWhile<TSource>(
        this IObservable<TSource> source, Func<TSource, bool> predicate)
    {
        return source.Subscribe().TakeWhile(predicate);
    }

    public static Operator<TakeWhileIndexed<Subscribe<TSource>, TSource>, TSource> TakeWhile<TSource>(
        this IObservable<TSource> source, Func<TSource, int, bool> predicate)
    {
        return source.Subscribe().TakeWhile(predicate);
    }

    public static Operator<Throttle<Subscribe<TSource>, TSource>, TSource> Throttle<TSource>(
        this IObservable<TSource> source, TimeSpan delay, bool continueOnCapturedContext = true)
    {
        return source.Subscribe().Throttle(delay, continueOnCapturedContext);
    }

    public static Operator<ToDictionary<Subscribe<KeyValuePair<TKey, TSource>>, TSource, TKey>, Dictionary<TKey, TSource>> ToDictionary<TSource, TKey>(
        this IObservable<KeyValuePair<TKey, TSource>> source)
        where TKey : notnull
    {
        return source.Subscribe().ToDictionary();
    }

    public static Operator<ToDictionary<Subscribe<KeyValuePair<TKey, TSource>>, TSource, TKey>, Dictionary<TKey, TSource>> ToDictionary<TSource, TKey>(
        this IObservable<KeyValuePair<TKey, TSource>> source, IEqualityComparer<TKey>? comparer)
        where TKey : notnull
    {
        return source.Subscribe().ToDictionary(comparer);
    }

    public static Operator<ToDictionary<Select<Subscribe<TSource>, TSource, KeyValuePair<TKey, TSource>>, TSource, TKey>, Dictionary<TKey, TSource>> ToDictionary<TSource, TKey>(
        this IObservable<TSource> source, Func<TSource, TKey> keySelector)
        where TKey : notnull
    {
        return source.Subscribe().ToDictionary(keySelector);
    }

    public static Operator<ToDictionary<Select<Subscribe<TSource>, TSource, KeyValuePair<TKey, TSource>>, TSource, TKey>, Dictionary<TKey, TSource>> ToDictionary<TSource, TKey>(
        this IObservable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey>? comparer)
        where TKey : notnull
    {
        return source.Subscribe().ToDictionary(keySelector, comparer);
    }

    public static Operator<ToDictionary<Select<Subscribe<TSource>, TSource, KeyValuePair<TKey, TValue>>, TValue, TKey>, Dictionary<TKey, TValue>> ToDictionary<TSource, TKey, TValue>(
        this IObservable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TValue> valueSelector)
        where TKey : notnull
    {
        return source.Subscribe().ToDictionary(keySelector, valueSelector);
    }

    public static Operator<ToDictionary<Select<Subscribe<TSource>, TSource, KeyValuePair<TKey, TValue>>, TValue, TKey>, Dictionary<TKey, TValue>> ToDictionary<TSource, TKey, TValue>(
        this IObservable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TValue> valueSelector, IEqualityComparer<TKey>? comparer)
        where TKey : notnull
    {
        return source.Subscribe().ToDictionary(keySelector, valueSelector, comparer);
    }

    public static Operator<ToList<Subscribe<TSource>, TSource>, List<TSource>> ToList<TSource>(
        this IObservable<TSource> source)
    {
        return source.Subscribe().ToList();
    }

    public static Operator<ToArray<Subscribe<TSource>, TSource>, TSource[]> ToArray<TSource>(
        this IObservable<TSource> source)
    {
        return source.Subscribe().ToArray();
    }

    public static Operator<OnCompleted<Subscribe<TSource>, TSource>, TSource> OnCompleted<TSource>(
        this IObservable<TSource> source, Action onCompleted)
    {
        return source.Subscribe().OnCompleted(onCompleted);
    }

    public static Operator<OnError<Subscribe<TSource>, TSource>, TSource> OnError<TSource>(
        this IObservable<TSource> source, Action<Exception> onError)
    {
        return source.Subscribe().OnError(onError);
    }

    public static Operator<OnNext<Subscribe<TSource>, TSource>, TSource> OnNext<TSource>(
        this IObservable<TSource> source, Action<TSource> onNext)
    {
        return source.Subscribe().OnNext(onNext);
    }

    public static Task<TResult> ToTask<TResult>(
        this IObservable<TResult> source)
    {
        return source.Subscribe().ToTask();
    }

    public static ValueTask<TResult> ToValueTask<TResult>(
        this IObservable<TResult> source)
    {
        return source.Subscribe().ToValueTask();
    }

    public static ValueTaskAwaiter<TResult> GetAwaiter<TResult>(
        this IObservable<TResult> source)
    {
        return source.Subscribe().GetAwaiter();
    }

    public static Operator<Subscribe<TSource>, TSource> Subscribe<TSource>(
        this IObservable<TSource> source)
    {
        return new(new(source));
    }

    public static IDisposable Subscribe<TSource>(
        this IObservable<TSource> source, Action<TSource> onNext)
    {
        return source.Subscribe().OnNext(onNext).Subscribe();
    }
}