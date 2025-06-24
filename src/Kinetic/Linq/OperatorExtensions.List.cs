using System;

namespace Kinetic.Linq;

public static partial class OperatorExtensions
{
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
}