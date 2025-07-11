using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Kinetic.Linq;

public static partial class OperatorExtensions
{
    public static Operator<Aggregate<TOperator, TSource>, TSource> Aggregate<TOperator, TSource>(
        this Operator<TOperator, TSource> source, Func<TSource, TSource, TSource> accumulator)
        where TOperator : IOperator<TSource>
    {
        return new(new(source, accumulator));
    }

    public static Operator<AggregateWithSeed<TOperator, TSource, TAccumulate>, TAccumulate> Aggregate<TOperator, TSource, TAccumulate>(
        this Operator<TOperator, TSource> source, TAccumulate seed, Func<TAccumulate, TSource, TAccumulate> accumulator)
        where TOperator : IOperator<TSource>
    {
        return new(new(source, seed, accumulator));
    }

    public static Operator<Select<AggregateWithSeed<TOperator, TSource, TAccumulate>, TAccumulate, TResult>, TResult> Aggregate<TOperator, TSource, TAccumulate, TResult>(
        this Operator<TOperator, TSource> source, TAccumulate seed, Func<TAccumulate?, TSource, TAccumulate> accumulator, Func<TAccumulate, TResult> selector)
        where TOperator : IOperator<TSource>
    {
        return source.Aggregate(seed, accumulator).Select(selector);
    }

    public static Operator<All<TOperator>, bool> All<TOperator>(
        this Operator<TOperator, bool> source)
        where TOperator : IOperator<bool>
    {
        return new(new(source));
    }

    public static Operator<All<Select<TOperator, TSource, bool>>, bool> All<TOperator, TSource>(
        this Operator<TOperator, TSource> source, Func<TSource, bool> predicate)
        where TOperator : IOperator<TSource>
    {
        return source.Select(predicate).All();
    }

    public static Operator<Any<TOperator, TSource>, bool> Any<TOperator, TSource>(
        this Operator<TOperator, TSource> source)
        where TOperator : IOperator<TSource>
    {
        return new(new(source));
    }

    public static Operator<Any<Where<TOperator, TSource>, TSource>, bool> Any<TOperator, TSource>(
        this Operator<TOperator, TSource> source, Func<TSource, bool> predicate)
        where TOperator : IOperator<TSource>
    {
        return source.Where(predicate).Any();
    }

    public static Operator<Contains<TOperator, TSource>, bool> Contains<TOperator, TSource>(
        this Operator<TOperator, TSource> source, TSource value)
        where TOperator : IOperator<TSource>
    {
        return source.Contains(value, comparer: null);
    }

    public static Operator<Contains<TOperator, TSource>, bool> Contains<TOperator, TSource>(
        this Operator<TOperator, TSource> source, TSource value, IEqualityComparer<TSource>? comparer)
        where TOperator : IOperator<TSource>
    {
        return new(new(source, value, comparer));
    }

    public static Operator<Count<TOperator, TSource>, int> Count<TOperator, TSource>(
        this Operator<TOperator, TSource> source)
        where TOperator : IOperator<TSource>
    {
        return new(new());
    }

    public static Operator<Count<Where<TOperator, TSource>, TSource>, int> Count<TOperator, TSource>(
        this Operator<TOperator, TSource> source, Func<TSource, bool> predicate)
        where TOperator : IOperator<TSource>
    {
        return source.Where(predicate).Count();
    }

    public static Operator<Distinct<TOperator, TSource>, TSource> Distinct<TOperator, TSource>(
        this Operator<TOperator, TSource> source)
        where TOperator : IOperator<TSource>
    {
        return source.Distinct(comparer: null);
    }

    public static Operator<Distinct<TOperator, TSource>, TSource> Distinct<TOperator, TSource>(
        this Operator<TOperator, TSource> source, IEqualityComparer<TSource>? comparer)
        where TOperator : IOperator<TSource>
    {
        return new(new(source, comparer));
    }

    public static Operator<DistinctBy<TOperator, TSource, TKey>, TSource> DistinctBy<TOperator, TSource, TKey>(
        this Operator<TOperator, TSource> source, Func<TSource, TKey> keySelector)
        where TOperator : IOperator<TSource>
    {
        return source.DistinctBy(keySelector, comparer: null);
    }

    public static Operator<DistinctBy<TOperator, TSource, TKey>, TSource> DistinctBy<TOperator, TSource, TKey>(
        this Operator<TOperator, TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey>? comparer)
        where TOperator : IOperator<TSource>
    {
        return new(new(source, keySelector, comparer));
    }

    public static Operator<First<TOperator, TSource>, TSource> First<TOperator, TSource>(
        this Operator<TOperator, TSource> source)
        where TOperator : IOperator<TSource>
    {
        return new(new(source));
    }

    public static Operator<First<Where<TOperator, TSource>, TSource>, TSource> First<TOperator, TSource>(
        this Operator<TOperator, TSource> source, Func<TSource, bool> predicate)
        where TOperator : IOperator<TSource>
    {
        return source.Where(predicate).First();
    }

    public static Operator<FirstOrDefault<Cast<TOperator, TSource, TSource?>, TSource?>, TSource?> FirstOrDefault<TOperator, TSource>(
        this Operator<TOperator, TSource> source)
        where TOperator : IOperator<TSource>
    {
        return new(new(source.Cast<TSource?>(), default));
    }

    public static Operator<FirstOrDefault<TOperator, TSource>, TSource> FirstOrDefault<TOperator, TSource>(
        this Operator<TOperator, TSource> source, TSource defaultValue)
        where TOperator : IOperator<TSource>
    {
        return new(new(source, defaultValue));
    }

    public static Operator<FirstOrDefault<Cast<Where<TOperator, TSource>, TSource, TSource?>, TSource?>, TSource?> FirstOrDefault<TOperator, TSource>(
        this Operator<TOperator, TSource> source, Func<TSource, bool> predicate)
        where TOperator : IOperator<TSource>
    {
        return source.Where(predicate).FirstOrDefault();
    }

    public static Operator<FirstOrDefault<Where<TOperator, TSource>, TSource>, TSource> FirstOrDefault<TOperator, TSource>(
        this Operator<TOperator, TSource> source, Func<TSource, bool> predicate, TSource defaultValue)
        where TOperator : IOperator<TSource>
    {
        return source.Where(predicate).FirstOrDefault(defaultValue);
    }

    public static Operator<Last<TOperator, TSource>, TSource> Last<TOperator, TSource>(
        this Operator<TOperator, TSource> source)
        where TOperator : IOperator<TSource>
    {
        return new(new(source));
    }

    public static Operator<Last<Where<TOperator, TSource>, TSource>, TSource> Last<TOperator, TSource>(
        this Operator<TOperator, TSource> source, Func<TSource, bool> predicate)
        where TOperator : IOperator<TSource>
    {
        return source.Where(predicate).Last();
    }

    public static Operator<LastOrDefault<Cast<TOperator, TSource, TSource?>, TSource?>, TSource?> LastOrDefault<TOperator, TSource>(
        this Operator<TOperator, TSource> source)
        where TOperator : IOperator<TSource>
    {
        return new(new(source.Cast<TSource?>(), default));
    }

    public static Operator<LastOrDefault<TOperator, TSource>, TSource> LastOrDefault<TOperator, TSource>(
        this Operator<TOperator, TSource> source, TSource defaultValue)
        where TOperator : IOperator<TSource>
    {
        return new(new(source, defaultValue));
    }

    public static Operator<LastOrDefault<Cast<Where<TOperator, TSource>, TSource, TSource?>, TSource?>, TSource?> LastOrDefault<TOperator, TSource>(
        this Operator<TOperator, TSource> source, Func<TSource, bool> predicate)
        where TOperator : IOperator<TSource>
    {
        return source.Where(predicate).LastOrDefault();
    }

    public static Operator<LastOrDefault<Where<TOperator, TSource>, TSource>, TSource> LastOrDefault<TOperator, TSource>(
        this Operator<TOperator, TSource> source, Func<TSource, bool> predicate, TSource defaultValue)
        where TOperator : IOperator<TSource>
    {
        return source.Where(predicate).LastOrDefault(defaultValue);
    }

    public static Operator<Single<TOperator, TSource>, TSource> Single<TOperator, TSource>(
        this Operator<TOperator, TSource> source)
        where TOperator : IOperator<TSource>
    {
        return new(new(source));
    }

    public static Operator<Single<Where<TOperator, TSource>, TSource>, TSource> Single<TOperator, TSource>(
        this Operator<TOperator, TSource> source, Func<TSource, bool> predicate)
        where TOperator : IOperator<TSource>
    {
        return source.Where(predicate).Single();
    }

    public static Operator<SingleOrDefault<Cast<TOperator, TSource, TSource?>, TSource?>, TSource?> SingleOrDefault<TOperator, TSource>(
        this Operator<TOperator, TSource> source)
        where TOperator : IOperator<TSource>
    {
        return new(new(source.Cast<TSource?>(), default));
    }

    public static Operator<SingleOrDefault<TOperator, TSource>, TSource> SingleOrDefault<TOperator, TSource>(
        this Operator<TOperator, TSource> source, TSource defaultValue)
        where TOperator : IOperator<TSource>
    {
        return new(new(source, defaultValue));
    }

    public static Operator<SingleOrDefault<Cast<Where<TOperator, TSource>, TSource, TSource?>, TSource?>, TSource?> SingleOrDefault<TOperator, TSource>(
        this Operator<TOperator, TSource> source, Func<TSource, bool> predicate)
        where TOperator : IOperator<TSource>
    {
        return source.Where(predicate).SingleOrDefault();
    }

    public static Operator<SingleOrDefault<Where<TOperator, TSource>, TSource>, TSource> SingleOrDefault<TOperator, TSource>(
        this Operator<TOperator, TSource> source, Func<TSource, bool> predicate, TSource defaultValue)
        where TOperator : IOperator<TSource>
    {
        return source.Where(predicate).SingleOrDefault(defaultValue);
    }

    public static Operator<Max<TOperator, TSource>, TSource> Max<TOperator, TSource>(
        this Operator<TOperator, TSource> source)
        where TOperator : IOperator<TSource>
    {
        return source.Max(comparer: null);
    }

    public static Operator<Max<TOperator, TSource>, TSource> Max<TOperator, TSource>(
        this Operator<TOperator, TSource> source, IComparer<TSource>? comparer)
        where TOperator : IOperator<TSource>
    {
        return new(new(source, comparer));
    }

    public static Operator<MaxBy<TOperator, TSource, TKey>, TSource> MaxBy<TOperator, TSource, TKey>(
        this Operator<TOperator, TSource> source, Func<TSource, TKey> keySelector)
        where TOperator : IOperator<TSource>
    {
        return source.MaxBy(keySelector, comparer: null);
    }

    public static Operator<MaxBy<TOperator, TSource, TKey>, TSource> MaxBy<TOperator, TSource, TKey>(
        this Operator<TOperator, TSource> source, Func<TSource, TKey> keySelector, IComparer<TKey>? comparer)
        where TOperator : IOperator<TSource>
    {
        return new(new(source, keySelector, comparer));
    }

    public static Operator<Min<TOperator, TSource>, TSource> Min<TOperator, TSource>(
        this Operator<TOperator, TSource> source)
        where TOperator : IOperator<TSource>
    {
        return source.Min(comparer: null);
    }

    public static Operator<Min<TOperator, TSource>, TSource> Min<TOperator, TSource>(
        this Operator<TOperator, TSource> source, IComparer<TSource>? comparer)
        where TOperator : IOperator<TSource>
    {
        return new(new(source, comparer));
    }

    public static Operator<MinBy<TOperator, TSource, TKey>, TSource> MinBy<TOperator, TSource, TKey>(
        this Operator<TOperator, TSource> source, Func<TSource, TKey> keySelector)
        where TOperator : IOperator<TSource>
    {
        return source.MinBy(keySelector, comparer: null);
    }

    public static Operator<MinBy<TOperator, TSource, TKey>, TSource> MinBy<TOperator, TSource, TKey>(
        this Operator<TOperator, TSource> source, Func<TSource, TKey> keySelector, IComparer<TKey>? comparer)
        where TOperator : IOperator<TSource>
    {
        return new(new(source, keySelector, comparer));
    }

    public static Operator<Select<TOperator, TSource, TResult>, TResult> Select<TOperator, TSource, TResult>(
        this Operator<TOperator, TSource> source, Func<TSource, TResult> selector)
        where TOperator : IOperator<TSource>
    {
        return new(new(source, selector));
    }

    public static Operator<SelectIndexed<TOperator, TSource, TResult>, TResult> Select<TOperator, TSource, TResult>(
        this Operator<TOperator, TSource> source, Func<TSource, int, TResult> selector)
        where TOperator : IOperator<TSource>
    {
        return new(new(source, selector));
    }

    public static Operator<SelectAwait<TOperator, TSource, TResult>, TResult> SelectAwait<TOperator, TSource, TResult>(
        this Operator<TOperator, TSource> source, Func<TSource, ValueTask<TResult>> selector)
        where TOperator : IOperator<TSource>
    {
        return new(new(source, selector));
    }

    public static Operator<SelectAwaitIndexed<TOperator, TSource, TResult>, TResult> SelectAwait<TOperator, TSource, TResult>(
        this Operator<TOperator, TSource> source, Func<TSource, int, ValueTask<TResult>> selector)
        where TOperator : IOperator<TSource>
    {
        return new(new(source, selector));
    }

    public static Operator<SelectAwaitTask<TOperator, TSource, TResult>, TResult> SelectAwait<TOperator, TSource, TResult>(
        this Operator<TOperator, TSource> source, Func<TSource, Task<TResult>> selector)
        where TOperator : IOperator<TSource>
    {
        return new(new(source, selector));
    }

    public static Operator<SelectAwaitTaskIndexed<TOperator, TSource, TResult>, TResult> SelectAwait<TOperator, TSource, TResult>(
        this Operator<TOperator, TSource> source, Func<TSource, int, Task<TResult>> selector)
        where TOperator : IOperator<TSource>
    {
        return new(new(source, selector));
    }

    public static Operator<Where<TOperator, TSource>, TSource> Where<TOperator, TSource>(
        this Operator<TOperator, TSource> source, Func<TSource, bool> predicate)
        where TOperator : IOperator<TSource>
    {
        return new(new(source, predicate));
    }

    public static Operator<WhereIndexed<TOperator, TSource>, TSource> Where<TOperator, TSource>(
        this Operator<TOperator, TSource> source, Func<TSource, int, bool> predicate)
        where TOperator : IOperator<TSource>
    {
        return new(new(source, predicate));
    }

    public static Operator<WhereAwait<TOperator, TSource>, TSource> WhereAwait<TOperator, TSource>(
        this Operator<TOperator, TSource> source, Func<TSource, ValueTask<bool>> predicate)
        where TOperator : IOperator<TSource>
    {
        return new(new(source, predicate));
    }

    public static Operator<WhereAwaitIndexed<TOperator, TSource>, TSource> WhereAwait<TOperator, TSource>(
        this Operator<TOperator, TSource> source, Func<TSource, int, ValueTask<bool>> predicate)
        where TOperator : IOperator<TSource>
    {
        return new(new(source, predicate));
    }

    public static Operator<WhereAwaitTask<TOperator, TSource>, TSource> WhereAwait<TOperator, TSource>(
        this Operator<TOperator, TSource> source, Func<TSource, Task<bool>> predicate)
        where TOperator : IOperator<TSource>
    {
        return new(new(source, predicate));
    }

    public static Operator<WhereAwaitTaskIndexed<TOperator, TSource>, TSource> WhereAwait<TOperator, TSource>(
        this Operator<TOperator, TSource> source, Func<TSource, int, Task<bool>> predicate)
        where TOperator : IOperator<TSource>
    {
        return new(new(source, predicate));
    }

    public static Operator<Skip<TOperator, TSource>, TSource> Skip<TOperator, TSource>(
        this Operator<TOperator, TSource> source, int count)
        where TOperator : IOperator<TSource>
    {
        return new(new(source, count));
    }

    public static Operator<SkipWhile<TOperator, TSource>, TSource> SkipWhile<TOperator, TSource>(
        this Operator<TOperator, TSource> source, Func<TSource, bool> predicate)
        where TOperator : IOperator<TSource>
    {
        return new(new(source, predicate));
    }

    public static Operator<SkipWhileIndexed<TOperator, TSource>, TSource> SkipWhile<TOperator, TSource>(
        this Operator<TOperator, TSource> source, Func<TSource, int, bool> predicate)
        where TOperator : IOperator<TSource>
    {
        return new(new(source, predicate));
    }

    public static Operator<Take<TOperator, TSource>, TSource> Take<TOperator, TSource>(
        this Operator<TOperator, TSource> source, int count)
        where TOperator : IOperator<TSource>
    {
        return new(new(source, count));
    }

    public static Operator<TakeWhile<TOperator, TSource>, TSource> TakeWhile<TOperator, TSource>(
        this Operator<TOperator, TSource> source, Func<TSource, bool> predicate)
        where TOperator : IOperator<TSource>
    {
        return new(new(source, predicate));
    }

    public static Operator<TakeWhileIndexed<TOperator, TSource>, TSource> TakeWhile<TOperator, TSource>(
        this Operator<TOperator, TSource> source, Func<TSource, int, bool> predicate)
        where TOperator : IOperator<TSource>
    {
        return new(new(source, predicate));
    }

    public static Operator<Switch<TOperator, TSource>, TSource> Switch<TOperator, TSource>(
        this Operator<TOperator, IObservable<TSource>?> source)
        where TOperator : IOperator<IObservable<TSource>?>
    {
        return new(new(source));
    }

    public static Operator<SwitchToProperty<TOperator, TSource>, TSource> Switch<TOperator, TSource>(
        this Operator<TOperator, Property<TSource>?> source)
        where TOperator : IOperator<Property<TSource>?>
    {
        return new(new(source));
    }

    public static Operator<SwitchToReadOnlyProperty<TOperator, TSource>, TSource> Switch<TOperator, TSource>(
        this Operator<TOperator, ReadOnlyProperty<TSource>?> source)
        where TOperator : IOperator<ReadOnlyProperty<TSource>?>
    {
        return new(new(source));
    }

    public static Operator<Throttle<TOperator, TSource>, TSource> Throttle<TOperator, TSource>(
        this Operator<TOperator, TSource> source, TimeSpan delay, bool continueOnCapturedContext = true)
        where TOperator : IOperator<TSource>
    {
        return new(new(source, delay, continueOnCapturedContext));
    }

    public static Operator<OnCompleted<TOperator, TSource>, TSource> OnCompleted<TOperator, TSource>(
        this Operator<TOperator, TSource> source, Action onCompleted)
        where TOperator : IOperator<TSource>
    {
        return new(new(source, onCompleted));
    }

    public static Operator<OnError<TOperator, TSource>, TSource> OnError<TOperator, TSource>(
        this Operator<TOperator, TSource> source, Action<Exception> onError)
        where TOperator : IOperator<TSource>
    {
        return new(new(source, onError));
    }

    public static Operator<OnNext<TOperator, TSource>, TSource> OnNext<TOperator, TSource>(
        this Operator<TOperator, TSource> source, Action<TSource> onNext)
        where TOperator : IOperator<TSource>
    {
        return new(new(source, onNext));
    }

    public static Operator<ToDictionary<TOperator, TSource, TKey>, Dictionary<TKey, TSource>> ToDictionary<TOperator, TSource, TKey>(
        this Operator<TOperator, KeyValuePair<TKey, TSource>> source)
        where TOperator : IOperator<KeyValuePair<TKey, TSource>>
        where TKey : notnull
    {
        return source.ToDictionary(comparer: null);
    }

    public static Operator<ToDictionary<TOperator, TSource, TKey>, Dictionary<TKey, TSource>> ToDictionary<TOperator, TSource, TKey>(
        this Operator<TOperator, KeyValuePair<TKey, TSource>> source, IEqualityComparer<TKey>? comparer)
        where TOperator : IOperator<KeyValuePair<TKey, TSource>>
        where TKey : notnull
    {
        return new(new(source, comparer));
    }

    public static Operator<ToDictionary<Select<TOperator, TSource, KeyValuePair<TKey, TSource>>, TSource, TKey>, Dictionary<TKey, TSource>> ToDictionary<TOperator, TSource, TKey>(
        this Operator<TOperator, TSource> source, Func<TSource, TKey> keySelector)
        where TOperator : IOperator<TSource>
        where TKey : notnull
    {
        return source.ToDictionary(keySelector, comparer: null);
    }

    public static Operator<ToDictionary<Select<TOperator, TSource, KeyValuePair<TKey, TSource>>, TSource, TKey>, Dictionary<TKey, TSource>> ToDictionary<TOperator, TSource, TKey>(
        this Operator<TOperator, TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey>? comparer)
        where TOperator : IOperator<TSource>
        where TKey : notnull
    {
        return source.Select(source => KeyValuePair.Create(keySelector(source), source)).ToDictionary(comparer);
    }

    public static Operator<ToDictionary<Select<TOperator, TSource, KeyValuePair<TKey, TValue>>, TValue, TKey>, Dictionary<TKey, TValue>> ToDictionary<TOperator, TSource, TKey, TValue>(
        this Operator<TOperator, TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TValue> valueSelector)
        where TOperator : IOperator<TSource>
        where TKey : notnull
    {
        return source.ToDictionary(keySelector, valueSelector, comparer: null);
    }

    public static Operator<ToDictionary<Select<TOperator, TSource, KeyValuePair<TKey, TValue>>, TValue, TKey>, Dictionary<TKey, TValue>> ToDictionary<TOperator, TSource, TKey, TValue>(
        this Operator<TOperator, TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TValue> valueSelector, IEqualityComparer<TKey>? comparer)
        where TOperator : IOperator<TSource>
        where TKey : notnull
    {
        return source.Select(source => KeyValuePair.Create(keySelector(source), valueSelector(source))).ToDictionary(comparer);
    }

    public static Operator<ToList<TOperator, TSource>, List<TSource>> ToList<TOperator, TSource>(
        this Operator<TOperator, TSource> source)
        where TOperator : IOperator<TSource>
    {
        return new(new(source));
    }

    public static Operator<ToArray<TOperator, TSource>, TSource[]> ToArray<TOperator, TSource>(
        this Operator<TOperator, TSource> source)
        where TOperator : IOperator<TSource>
    {
        return new(new(source));
    }

    public static IObservable<TResult> ToObservable<TOperator, TResult>(
        this Operator<TOperator, TResult> source)
        where TOperator : IOperator<TResult>
    {
        return ObservableFactory<TResult>.Create<TOperator>(source);
    }

    public static Task<TResult> ToTask<TOperator, TResult>(
        this Operator<TOperator, TResult> source)
        where TOperator : IOperator<TResult>
    {
        return TaskFactory<TResult>.Create(source);
    }

    public static ValueTask<TResult> ToValueTask<TOperator, TResult>(
        this Operator<TOperator, TResult> source)
        where TOperator : IOperator<TResult>
    {
        return ValueTaskFactory<TResult>.Create(source);
    }

    public static ValueTaskAwaiter<TResult> GetAwaiter<TOperator, TResult>(
        this Operator<TOperator, TResult> source)
        where TOperator : IOperator<TResult>
    {
        return source.ToValueTask().GetAwaiter();
    }

    public static IDisposable Subscribe<TOperator, TResult>(
        this Operator<TOperator, TResult> source)
        where TOperator : IOperator<TResult>
    {
        return ObserverFactory<TResult>.Create<TOperator>(source);
    }

    public static IDisposable Subscribe<TOperator, TResult>(
        this Operator<TOperator, TResult> source, Action<TResult> onNext)
        where TOperator : IOperator<TResult>
    {
        return source.OnNext(onNext).Subscribe();
    }
}