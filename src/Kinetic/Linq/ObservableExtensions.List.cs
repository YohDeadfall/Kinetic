using System;

namespace Kinetic.Linq;

public static partial class ObservableExtensions
{
    public static Operator<SelectItem<Subscribe<ListChange<TSource>>, TSource, TResult>, ListChange<TResult>> Select<TOperator, TSource, TResult>(
        this IObservable<ListChange<TSource>> source, Func<TSource, TResult> selector)
        where TOperator : IOperator<ListChange<TSource>>
    {
        return source.Subscribe().Select(selector);
    }

    public static Operator<WhereItem<Subscribe<ListChange<TSource>>, TSource>, ListChange<TSource>> Where<TOperator, TSource>(
        this IObservable<ListChange<TSource>> source, Func<TSource, bool> predicate)
        where TOperator : IOperator<ListChange<TSource>>
    {
        return source.Subscribe().Where(predicate);
    }

    public static Operator<OnItemAdded<Subscribe<ListChange<TSource>>, TSource>, ListChange<TSource>> OnItemAdded<TOperator, TSource>(
        this IObservable<ListChange<TSource>> source, Action<TSource> onAdded)
        where TOperator : IOperator<ListChange<TSource>>
    {
        return source.Subscribe().OnItemAdded(onAdded);
    }

    public static Operator<OnItemRemoved<Subscribe<ListChange<TSource>>, TSource>, ListChange<TSource>> OnItemRemoved<TOperator, TSource>(
        this IObservable<ListChange<TSource>> source, Action<TSource> onRemoved)
        where TOperator : IOperator<ListChange<TSource>>
    {
        return source.Subscribe().OnItemRemoved(onRemoved);
    }

    public static Operator<OnItemRemoved<Subscribe<ListChange<TSource>>, TSource>, ListChange<TSource>> OnItemRemovedDispose<TOperator, TSource>(
        this IObservable<ListChange<TSource>> source)
        where TOperator : IOperator<ListChange<TSource>>
        where TSource : IDisposable
    {
        return source.Subscribe().OnItemRemovedDispose();
    }
}