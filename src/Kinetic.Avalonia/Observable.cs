using System;
using Avalonia.Threading;

namespace Kinetic.Linq;

public static class Observable
{
    public static Operator<ContinueOnDispatcher<TOperator, TSource>, TSource> ContinueOn<TOperator, TSource>(
        this Operator<TOperator, TSource> source, Dispatcher dispatcher)
        where TOperator : IOperator<TSource>
    {
        return source.ContinueOn(dispatcher, DispatcherPriority.Default);
    }

    public static Operator<ContinueOnDispatcher<TOperator, TSource>, TSource> ContinueOn<TOperator, TSource>(
        this Operator<TOperator, TSource> source, Dispatcher dispatcher, DispatcherPriority priority)
        where TOperator : IOperator<TSource>
    {
        return new(new(source, dispatcher, priority));
    }

    public static Operator<ContinueOnDispatcher<Subscribe<TSource>, TSource>, TSource> ContinueOn<TSource>(
        this IObservable<TSource> source, Dispatcher dispatcher)
    {
        return source.ContinueOn(dispatcher, DispatcherPriority.Default);
    }

    public static Operator<ContinueOnDispatcher<Subscribe<TSource>, TSource>, TSource> ContinueOn<TSource>(
        this IObservable<TSource> source, Dispatcher dispatcher, DispatcherPriority priority)
    {
        return source.Subscribe().ContinueOn(dispatcher, priority);
    }
}