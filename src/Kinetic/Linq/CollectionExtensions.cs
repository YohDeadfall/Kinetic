using System;
using System.Collections.Generic;

namespace Kinetic.Linq;

public static class CollectionExtensions
{
    public static Operator<GroupItemsBy<Subscribe<ListChange<TSource>>, TSource, TKey, ObservableGroup<TKey, TSource>>, ListChange<ObservableGroup<TKey, TSource>>> GroupBy<TSource, TKey>(
        this ReadOnlyObservableList<TSource> source, Func<TSource, TKey> keySelector)
    {
        return source.Changed.GroupBy(keySelector);
    }

    public static Operator<GroupItemsBy<Subscribe<ListChange<TSource>>, TSource, TKey, ObservableGroup<TKey, TSource>>, ListChange<ObservableGroup<TKey, TSource>>> GroupBy<TSource, TKey>(
        this ReadOnlyObservableList<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey>? comparer)
    {
        return source.Changed.GroupBy(keySelector, comparer);
    }

    public static Operator<GroupItemsBy<Subscribe<ListChange<TSource>>, TSource, TKey, TResult>, ListChange<TResult>> GroupBy<TSource, TKey, TResult>(
        this ReadOnlyObservableList<TSource> source,
        Func<TSource, TKey> keySelector,
        Func<IGrouping<TKey, ListChange<TSource>>, TResult> resultSelector)
    {
        return source.Changed.GroupBy(keySelector, resultSelector);
    }

    public static Operator<GroupItemsBy<Subscribe<ListChange<TSource>>, TSource, TKey, TResult>, ListChange<TResult>> GroupBy<TSource, TKey, TResult>(
        this ReadOnlyObservableList<TSource> source,
        Func<TSource, TKey> keySelector,
        Func<IGrouping<TKey, ListChange<TSource>>, TResult> resultSelector,
        IEqualityComparer<TKey>? comparer)
    {
        return source.Changed.GroupBy(keySelector, resultSelector, comparer);
    }

    public static Operator<GroupItemsByObservable<Subscribe<ListChange<TSource>>, TSource, TKey, ObservableGroup<TKey, TSource>>, ListChange<ObservableGroup<TKey, TSource>>> GroupByObservable<TSource, TKey>(
        this ReadOnlyObservableList<TSource> source, Func<TSource, IObservable<TKey>> keySelector)
    {
        return source.Changed.GroupByObservable(keySelector);
    }

    public static Operator<GroupItemsByObservable<Subscribe<ListChange<TSource>>, TSource, TKey, ObservableGroup<TKey, TSource>>, ListChange<ObservableGroup<TKey, TSource>>> GroupByObservable<TSource, TKey>(
        this ReadOnlyObservableList<TSource> source, Func<TSource, IObservable<TKey>> keySelector, IEqualityComparer<TKey>? comparer)
    {
        return source.Changed.GroupByObservable(keySelector, comparer);
    }

    public static Operator<GroupItemsByObservable<Subscribe<ListChange<TSource>>, TSource, TKey, TResult>, ListChange<TResult>> GroupByObservable<TSource, TKey, TResult>(
        this ReadOnlyObservableList<TSource> source,
        Func<TSource, IObservable<TKey>> keySelector,
        Func<IGrouping<TKey, ListChange<TSource>>, TResult> resultSelector)
    {
        return source.Changed.GroupByObservable(keySelector, resultSelector);
    }

    public static Operator<GroupItemsByObservable<Subscribe<ListChange<TSource>>, TSource, TKey, TResult>, ListChange<TResult>> GroupByObservable<TSource, TKey, TResult>(
        this ReadOnlyObservableList<TSource> source,
        Func<TSource, IObservable<TKey>> keySelector,
        Func<IGrouping<TKey, ListChange<TSource>>, TResult> resultSelector,
        IEqualityComparer<TKey>? comparer)
    {
        return source.Changed.GroupByObservable(keySelector, resultSelector, comparer);
    }

    public static Operator<OrderItemsBy<Subscribe<ListChange<TSource>>, TSource, TKey>, ListChange<TSource>> OrderBy<TSource, TKey>(
        this ReadOnlyObservableList<TSource> source, Func<TSource, TKey> keySelector)
    {
        return source.Changed.OrderBy(keySelector);
    }

    public static Operator<OrderItemsBy<Subscribe<ListChange<TSource>>, TSource, TKey>, ListChange<TSource>> OrderBy<TSource, TKey>(
        this ReadOnlyObservableList<TSource> source, Func<TSource, TKey> keySelector, IComparer<TKey>? comparer)
    {
        return source.Changed.OrderBy(keySelector, comparer);
    }

    public static Operator<OrderItemsByObservable<Subscribe<ListChange<TSource>>, TSource, TKey>, ListChange<TSource>> OrderByObservable<TSource, TKey>(
        this ReadOnlyObservableList<TSource> source, Func<TSource, IObservable<TKey>> keySelector)
    {
        return source.Changed.OrderByObservable(keySelector);
    }

    public static Operator<OrderItemsByObservable<Subscribe<ListChange<TSource>>, TSource, TKey>, ListChange<TSource>> OrderByObservable<TSource, TKey>(
        this ReadOnlyObservableList<TSource> source, Func<TSource, IObservable<TKey>> keySelector, IComparer<TKey>? comparer)
    {
        return source.Changed.OrderByObservable(keySelector, comparer);
    }

    public static Operator<SelectItems<Subscribe<ListChange<TSource>>, TSource, TResult>, ListChange<TResult>> Select<TSource, TResult>(
        this ReadOnlyObservableList<TSource> source, Func<TSource, TResult> selector)
    {
        return source.Changed.Select(selector);
    }

    public static Operator<SelectObservableItems<Subscribe<ListChange<TSource>>, TSource, TResult>, ListChange<TResult>> SelectObservable<TSource, TResult>(
        this ReadOnlyObservableList<TSource> source, Func<TSource, IObservable<TResult>> selector)
    {
        return source.Changed.SelectObservable(selector);
    }

    public static Operator<WhereItems<Subscribe<ListChange<TSource>>, TSource>, ListChange<TSource>> Where<TSource>(
        this ReadOnlyObservableList<TSource> source, Func<TSource, bool> predicate)
    {
        return source.Changed.Where(predicate);
    }

    public static Operator<WhereObservableItems<Subscribe<ListChange<TSource>>, TSource>, ListChange<TSource>> WhereObservable<TSource>(
        this ReadOnlyObservableList<TSource> source, Func<TSource, IObservable<bool>> predicate)
    {
        return source.Changed.WhereObservable(predicate);
    }

    public static Operator<OnItemAdded<Subscribe<ListChange<TSource>>, TSource>, ListChange<TSource>> OnItemAdded<TSource>(
        this ReadOnlyObservableList<TSource> source, Action<TSource> onAdded)
    {
        return source.Changed.OnItemAdded(onAdded);
    }

    public static Operator<OnItemRemoved<Subscribe<ListChange<TSource>>, TSource>, ListChange<TSource>> OnItemRemoved<TSource>(
        this ReadOnlyObservableList<TSource> source, Action<TSource> onRemoved)
    {
        return source.Changed.OnItemRemoved(onRemoved);
    }

    public static Operator<OnItemRemoved<Subscribe<ListChange<TSource>>, TSource>, ListChange<TSource>> OnItemRemovedDispose<TSource>(
        this ReadOnlyObservableList<TSource> source)
        where TSource : IDisposable
    {
        return source.Changed.OnItemRemovedDispose();
    }
}