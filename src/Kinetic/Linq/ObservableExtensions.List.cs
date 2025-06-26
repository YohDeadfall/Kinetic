using System;
using System.Collections.Generic;

namespace Kinetic.Linq;

public static partial class ObservableExtensions
{
    public static Operator<GroupItemsBy<Subscribe<ListChange<TSource>>, TSource, TKey, ObservableGroup<TKey, TSource>>, ListChange<ObservableGroup<TKey, TSource>>> GroupBy<TSource, TKey>(
        this IObservable<ListChange<TSource>> source, Func<TSource, TKey> keySelector)
    {
        return source.Subscribe().GroupBy(keySelector);
    }

    public static Operator<GroupItemsBy<Subscribe<ListChange<TSource>>, TSource, TKey, ObservableGroup<TKey, TSource>>, ListChange<ObservableGroup<TKey, TSource>>> GroupBy<TSource, TKey>(
        this IObservable<ListChange<TSource>> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey>? comparer)
    {
        return source.Subscribe().GroupBy(keySelector, comparer);
    }

    public static Operator<GroupItemsBy<Subscribe<ListChange<TSource>>, TSource, TKey, TResult>, ListChange<TResult>> GroupBy<TSource, TKey, TResult>(
        this IObservable<ListChange<TSource>> source,
        Func<TSource, TKey> keySelector,
        Func<IGrouping<TKey, ListChange<TSource>>, TResult> resultSelector)
    {
        return source.Subscribe().GroupBy(keySelector, resultSelector);
    }

    public static Operator<GroupItemsBy<Subscribe<ListChange<TSource>>, TSource, TKey, TResult>, ListChange<TResult>> GroupBy<TSource, TKey, TResult>(
        this IObservable<ListChange<TSource>> source,
        Func<TSource, TKey> keySelector,
        Func<IGrouping<TKey, ListChange<TSource>>, TResult> resultSelector,
        IEqualityComparer<TKey>? comparer)
    {
        return source.Subscribe().GroupBy(keySelector, resultSelector, comparer);
    }

    public static Operator<GroupItemsByObservable<Subscribe<ListChange<TSource>>, TSource, TKey, ObservableGroup<TKey, TSource>>, ListChange<ObservableGroup<TKey, TSource>>> GroupBy<TSource, TKey>(
        this IObservable<ListChange<TSource>> source, Func<TSource, IObservable<TKey>> keySelector)
    {
        return source.Subscribe().GroupBy(keySelector);
    }

    public static Operator<GroupItemsByObservable<Subscribe<ListChange<TSource>>, TSource, TKey, ObservableGroup<TKey, TSource>>, ListChange<ObservableGroup<TKey, TSource>>> GroupBy<TSource, TKey>(
        this IObservable<ListChange<TSource>> source, Func<TSource, IObservable<TKey>> keySelector, IEqualityComparer<TKey>? comparer)
    {
        return source.Subscribe().GroupBy(keySelector, comparer);
    }

    public static Operator<GroupItemsByObservable<Subscribe<ListChange<TSource>>, TSource, TKey, TResult>, ListChange<TResult>> GroupBy<TSource, TKey, TResult>(
        this IObservable<ListChange<TSource>> source,
        Func<TSource, IObservable<TKey>> keySelector,
        Func<IGrouping<TKey, ListChange<TSource>>, TResult> resultSelector)
    {
        return source.Subscribe().GroupBy(keySelector, resultSelector);
    }

    public static Operator<GroupItemsByObservable<Subscribe<ListChange<TSource>>, TSource, TKey, TResult>, ListChange<TResult>> GroupBy<TSource, TKey, TResult>(
        this IObservable<ListChange<TSource>> source,
        Func<TSource, IObservable<TKey>> keySelector,
        Func<IGrouping<TKey, ListChange<TSource>>, TResult> resultSelector,
        IEqualityComparer<TKey>? comparer)
    {
        return source.Subscribe().GroupBy(keySelector, resultSelector, comparer);
    }

    public static Operator<OrderItemsBy<Subscribe<ListChange<TSource>>, TSource, TKey>, ListChange<TSource>> OrderBy<TSource, TKey>(
        this IObservable<ListChange<TSource>> source, Func<TSource, TKey> keySelector)
    {
        return source.Subscribe().OrderBy(keySelector);
    }

    public static Operator<OrderItemsBy<Subscribe<ListChange<TSource>>, TSource, TKey>, ListChange<TSource>> OrderBy<TSource, TKey>(
        this IObservable<ListChange<TSource>> source, Func<TSource, TKey> keySelector, IComparer<TKey>? comparer)
    {
        return source.Subscribe().OrderBy(keySelector, comparer);
    }

    public static Operator<OrderItemsByObservable<Subscribe<ListChange<TSource>>, TSource, TKey>, ListChange<TSource>> OrderByObservable<TSource, TKey>(
        this IObservable<ListChange<TSource>> source, Func<TSource, IObservable<TKey>> keySelector)
    {
        return source.Subscribe().OrderByObservable(keySelector);
    }

    public static Operator<OrderItemsByObservable<Subscribe<ListChange<TSource>>, TSource, TKey>, ListChange<TSource>> OrderByObservable<TSource, TKey>(
        this IObservable<ListChange<TSource>> source, Func<TSource, IObservable<TKey>> keySelector, IComparer<TKey>? comparer)
    {
        return source.Subscribe().OrderByObservable(keySelector, comparer);
    }

    public static Operator<SelectItem<Subscribe<ListChange<TSource>>, TSource, TResult>, ListChange<TResult>> Select<TSource, TResult>(
        this IObservable<ListChange<TSource>> source, Func<TSource, TResult> selector)
    {
        return source.Subscribe().Select(selector);
    }

    public static Operator<SelectObservableItems<Subscribe<ListChange<TSource>>, TSource, TResult>, ListChange<TResult>> SelectObservable<TSource, TResult>(
        this IObservable<ListChange<TSource>> source, Func<TSource, IObservable<TResult>> selector)
    {
        return source.Subscribe().SelectObservable(selector);
    }

    public static Operator<WhereItem<Subscribe<ListChange<TSource>>, TSource>, ListChange<TSource>> Where<TSource>(
        this IObservable<ListChange<TSource>> source, Func<TSource, bool> predicate)
    {
        return source.Subscribe().Where(predicate);
    }

    public static Operator<OnItemAdded<Subscribe<ListChange<TSource>>, TSource>, ListChange<TSource>> OnItemAdded<TSource>(
        this IObservable<ListChange<TSource>> source, Action<TSource> onAdded)
    {
        return source.Subscribe().OnItemAdded(onAdded);
    }

    public static Operator<OnItemRemoved<Subscribe<ListChange<TSource>>, TSource>, ListChange<TSource>> OnItemRemoved<TSource>(
        this IObservable<ListChange<TSource>> source, Action<TSource> onRemoved)
    {
        return source.Subscribe().OnItemRemoved(onRemoved);
    }

    public static Operator<OnItemRemoved<Subscribe<ListChange<TSource>>, TSource>, ListChange<TSource>> OnItemRemovedDispose<TSource>(
        this IObservable<ListChange<TSource>> source)
        where TSource : IDisposable
    {
        return source.Subscribe().OnItemRemovedDispose();
    }
}