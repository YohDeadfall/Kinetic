using System;
using System.Collections.Generic;

namespace Kinetic.Linq;

public static partial class OperatorExtensions
{
    public static Operator<GroupItemsBy<TOperator, TSource, TKey, ObservableGroup<TKey, TSource>>, ListChange<ObservableGroup<TKey, TSource>>> GroupBy<TOperator, TSource, TKey>(
        this Operator<TOperator, ListChange<TSource>> source, Func<TSource, TKey> keySelector)
        where TOperator : IOperator<ListChange<TSource>>
    {
        return source.GroupBy(keySelector, comparer: null);
    }

    public static Operator<GroupItemsBy<TOperator, TSource, TKey, ObservableGroup<TKey, TSource>>, ListChange<ObservableGroup<TKey, TSource>>> GroupBy<TOperator, TSource, TKey>(
        this Operator<TOperator, ListChange<TSource>> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey>? comparer)
        where TOperator : IOperator<ListChange<TSource>>
    {
        return source.GroupBy(keySelector, grouping => grouping.Subscribe().ToGroup(grouping.Key), comparer);
    }

    public static Operator<GroupItemsBy<TOperator, TSource, TKey, TResult>, ListChange<TResult>> GroupBy<TOperator, TSource, TKey, TResult>(
        this Operator<TOperator, ListChange<TSource>> source,
        Func<TSource, TKey> keySelector,
        Func<IGrouping<TKey, ListChange<TSource>>, TResult> resultSelector)
        where TOperator : IOperator<ListChange<TSource>>
    {
        return source.GroupBy(keySelector, resultSelector, comparer: null);
    }

    public static Operator<GroupItemsBy<TOperator, TSource, TKey, TResult>, ListChange<TResult>> GroupBy<TOperator, TSource, TKey, TResult>(
        this Operator<TOperator, ListChange<TSource>> source,
        Func<TSource, TKey> keySelector,
        Func<IGrouping<TKey, ListChange<TSource>>, TResult> resultSelector,
        IEqualityComparer<TKey>? comparer)
        where TOperator : IOperator<ListChange<TSource>>
    {
        return new(new(source, keySelector, resultSelector, comparer));
    }

    public static Operator<GroupItemsByObservable<TOperator, TSource, TKey, ObservableGroup<TKey, TSource>>, ListChange<ObservableGroup<TKey, TSource>>> GroupByObservable<TOperator, TSource, TKey>(
        this Operator<TOperator, ListChange<TSource>> source, Func<TSource, IObservable<TKey>> keySelector)
        where TOperator : IOperator<ListChange<TSource>>
    {
        return source.GroupByObservable(keySelector, comparer: null);
    }

    public static Operator<GroupItemsByObservable<TOperator, TSource, TKey, ObservableGroup<TKey, TSource>>, ListChange<ObservableGroup<TKey, TSource>>> GroupByObservable<TOperator, TSource, TKey>(
        this Operator<TOperator, ListChange<TSource>> source, Func<TSource, IObservable<TKey>> keySelector, IEqualityComparer<TKey>? comparer)
        where TOperator : IOperator<ListChange<TSource>>
    {
        return source.GroupByObservable(keySelector, grouping => grouping.Subscribe().ToGroup(grouping.Key), comparer);
    }

    public static Operator<GroupItemsByObservable<TOperator, TSource, TKey, TResult>, ListChange<TResult>> GroupByObservable<TOperator, TSource, TKey, TResult>(
        this Operator<TOperator, ListChange<TSource>> source,
        Func<TSource, IObservable<TKey>> keySelector,
        Func<IGrouping<TKey, ListChange<TSource>>, TResult> resultSelector)
        where TOperator : IOperator<ListChange<TSource>>
    {
        return source.GroupByObservable(keySelector, resultSelector, comparer: null);
    }

    public static Operator<GroupItemsByObservable<TOperator, TSource, TKey, TResult>, ListChange<TResult>> GroupByObservable<TOperator, TSource, TKey, TResult>(
        this Operator<TOperator, ListChange<TSource>> source,
        Func<TSource, IObservable<TKey>> keySelector,
        Func<IGrouping<TKey, ListChange<TSource>>, TResult> resultSelector,
        IEqualityComparer<TKey>? comparer)
        where TOperator : IOperator<ListChange<TSource>>
    {
        return new(new(source, keySelector, resultSelector, comparer));
    }

    public static Operator<OrderItemsBy<TOperator, TSource, TKey>, ListChange<TSource>> OrderBy<TOperator, TSource, TKey>(
        this Operator<TOperator, ListChange<TSource>> source, Func<TSource, TKey> keySelector)
        where TOperator : IOperator<ListChange<TSource>>
    {
        return source.OrderBy(keySelector, comparer: null);
    }

    public static Operator<OrderItemsBy<TOperator, TSource, TKey>, ListChange<TSource>> OrderBy<TOperator, TSource, TKey>(
        this Operator<TOperator, ListChange<TSource>> source, Func<TSource, TKey> keySelector, IComparer<TKey>? comparer)
        where TOperator : IOperator<ListChange<TSource>>
    {
        return new(new(source, keySelector, comparer));
    }

    public static Operator<OrderItemsByObservable<TOperator, TSource, TKey>, ListChange<TSource>> OrderByObservable<TOperator, TSource, TKey>(
        this Operator<TOperator, ListChange<TSource>> source, Func<TSource, IObservable<TKey>> keySelector)
        where TOperator : IOperator<ListChange<TSource>>
    {
        return source.OrderByObservable(keySelector, comparer: null);
    }

    public static Operator<OrderItemsByObservable<TOperator, TSource, TKey>, ListChange<TSource>> OrderByObservable<TOperator, TSource, TKey>(
        this Operator<TOperator, ListChange<TSource>> source, Func<TSource, IObservable<TKey>> keySelector, IComparer<TKey>? comparer)
        where TOperator : IOperator<ListChange<TSource>>
    {
        return new(new(source, keySelector, comparer));
    }

    public static Operator<SelectItem<TOperator, TSource, TResult>, ListChange<TResult>> Select<TOperator, TSource, TResult>(
        this Operator<TOperator, ListChange<TSource>> source, Func<TSource, TResult> selector)
        where TOperator : IOperator<ListChange<TSource>>
    {
        return new(new(source, selector));
    }

    public static Operator<SelectObservableItems<TOperator, TSource, TResult>, ListChange<TResult>> SelectObservable<TOperator, TSource, TResult>(
        this Operator<TOperator, ListChange<TSource>> source, Func<TSource, IObservable<TResult>> selector)
        where TOperator : IOperator<ListChange<TSource>>
    {
        return new(new(source, selector));
    }

    public static Operator<WhereItem<TOperator, TSource>, ListChange<TSource>> Where<TOperator, TSource>(
        this Operator<TOperator, ListChange<TSource>> source, Func<TSource, bool> predicate)
        where TOperator : IOperator<ListChange<TSource>>
    {
        return new(new(source, predicate));
    }

    public static Operator<OnItemAdded<TOperator, TSource>, ListChange<TSource>> OnItemAdded<TOperator, TSource>(
        this Operator<TOperator, ListChange<TSource>> source, Action<TSource> onAdded)
        where TOperator : IOperator<ListChange<TSource>>
    {
        return new(new(source, onAdded));
    }

    public static Operator<OnItemRemoved<TOperator, TSource>, ListChange<TSource>> OnItemRemoved<TOperator, TSource>(
        this Operator<TOperator, ListChange<TSource>> source, Action<TSource> onRemoved)
        where TOperator : IOperator<ListChange<TSource>>
    {
        return new(new(source, onRemoved));
    }

    public static Operator<OnItemRemoved<TOperator, TSource>, ListChange<TSource>> OnItemRemovedDispose<TOperator, TSource>(
        this Operator<TOperator, ListChange<TSource>> source)
        where TOperator : IOperator<ListChange<TSource>>
        where TSource : IDisposable
    {
        return source.OnItemRemoved(item => item?.Dispose());
    }

    public static ObservableView<TElement> ToView<TOperator, TElement>(
        this Operator<TOperator, ListChange<TElement>> source)
        where TOperator : IOperator<ListChange<TElement>>
    {
        var view = new ObservableView<TElement>();
        view.Bind(source);
        return view;
    }

    public static ObservableGroup<TKey, TElement> ToGroup<TOperator, TElement, TKey>(
        this Operator<TOperator, ListChange<TElement>> source, TKey key)
        where TOperator : IOperator<ListChange<TElement>>
    {
        var group = new ObservableGroup<TKey, TElement>(key);
        group.Bind(source);
        return group;
    }
}