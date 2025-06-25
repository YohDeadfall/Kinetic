using System;
using System.Collections.Generic;

namespace Kinetic.Linq;

public static partial class OperatorExtensions
{
    public static Operator<GroupItemsBy<TOperator, TSource, TKey, ObservableGrouping<TKey, TSource>>, ListChange<ObservableGrouping<TKey, TSource>>> GroupBy<TOperator, TSource, TKey>(
        this Operator<TOperator, ListChange<TSource>> source, Func<TSource, TKey> keySelector)
        where TOperator : IOperator<ListChange<TSource>>
    {
        return source.GroupBy(keySelector, comparer: null);
    }

    public static Operator<GroupItemsBy<TOperator, TSource, TKey, ObservableGrouping<TKey, TSource>>, ListChange<ObservableGrouping<TKey, TSource>>> GroupBy<TOperator, TSource, TKey>(
        this Operator<TOperator, ListChange<TSource>> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey>? comparer)
        where TOperator : IOperator<ListChange<TSource>>
    {
        return source.GroupBy(keySelector, grouping => new ObservableGrouping<TKey, TSource>(grouping.Key, grouping), comparer);
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

    public static Operator<GroupItemsBy<TOperator, TSource, TKey, ObservableGrouping<TKey, TSource>>, ListChange<ObservableGrouping<TKey, TSource>>> GroupBy<TOperator, TSource, TKey>(
        this Operator<TOperator, ListChange<TSource>> source, Func<TSource, IObservable<TKey>> keySelector)
        where TOperator : IOperator<ListChange<TSource>>
    {
        // FIXME: It could be a call of the follow up method, but the compiler CS0121.
        // It's clear that there's only one possible resolution as TKey isn't an observable.
        return source.GroupBy(keySelector, grouping => new ObservableGrouping<TKey, TSource>(grouping.Key, grouping), comparer: null);
    }

    public static Operator<GroupItemsBy<TOperator, TSource, TKey, ObservableGrouping<TKey, TSource>>, ListChange<ObservableGrouping<TKey, TSource>>> GroupBy<TOperator, TSource, TKey>(
        this Operator<TOperator, ListChange<TSource>> source, Func<TSource, IObservable<TKey>> keySelector, IEqualityComparer<TKey>? comparer)
        where TOperator : IOperator<ListChange<TSource>>
    {
        return source.GroupBy(keySelector, grouping => new ObservableGrouping<TKey, TSource>(grouping.Key, grouping), comparer);
    }

    public static Operator<GroupItemsBy<TOperator, TSource, TKey, TResult>, ListChange<TResult>> GroupBy<TOperator, TSource, TKey, TResult>(
        this Operator<TOperator, ListChange<TSource>> source,
        Func<TSource, IObservable<TKey>> keySelector,
        Func<IGrouping<TKey, ListChange<TSource>>, TResult> resultSelector)
        where TOperator : IOperator<ListChange<TSource>>
    {
        return source.GroupBy(keySelector, resultSelector, comparer: null);
    }

    public static Operator<GroupItemsBy<TOperator, TSource, TKey, TResult>, ListChange<TResult>> GroupBy<TOperator, TSource, TKey, TResult>(
        this Operator<TOperator, ListChange<TSource>> source,
        Func<TSource, IObservable<TKey>> keySelector,
        Func<IGrouping<TKey, ListChange<TSource>>, TResult> resultSelector,
        IEqualityComparer<TKey>? comparer)
        where TOperator : IOperator<ListChange<TSource>>
    {
        return new(new(source, keySelector, resultSelector, comparer));
    }

    public static Operator<SelectItem<TOperator, TSource, TResult>, ListChange<TResult>> Select<TOperator, TSource, TResult>(
        this Operator<TOperator, ListChange<TSource>> source, Func<TSource, TResult> selector)
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

    public static ObservableView<TResult> ToView<TOperator, TResult>(
        this Operator<TOperator, ListChange<TResult>> source)
        where TOperator : IOperator<ListChange<TResult>>
    {
        return new ObservableView<TResult>(source.ToObservable());
    }
}