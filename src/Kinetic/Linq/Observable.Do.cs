using System;
using Kinetic.Linq.StateMachines;

namespace Kinetic.Linq;

public static partial class Observable
{
    public static ObserverBuilder<TSource> Do<TSource>(this IObservable<TSource> source, Action<TSource> onNext) =>
        source.ToBuilder().Do(onNext);

    public static ObserverBuilder<TSource> Do<TSource>(this IObservable<TSource> source, Action<TSource> onNext, Action<Exception> onError) =>
        source.ToBuilder().Do(onNext, onError);

    public static ObserverBuilder<TSource> Do<TSource>(this IObservable<TSource> source, Action<TSource> onNext, Action onCompleted) =>
        source.ToBuilder().Do(onNext, onCompleted);

    public static ObserverBuilder<TSource> Do<TSource>(this IObservable<TSource> source, Action<TSource> onNext, Action<Exception> onError, Action onCompleted) =>
        source.ToBuilder().Do(onNext, onError, onCompleted);

    public static ObserverBuilder<TSource> Do<TSource>(this ObserverBuilder<TSource> source, Action<TSource> onNext) =>
        source.OnNext(onNext);

    public static ObserverBuilder<TSource> Do<TSource>(this ObserverBuilder<TSource> source, Action<TSource> onNext, Action<Exception> onError) =>
        source.OnError(onError).OnNext(onNext);

    public static ObserverBuilder<TSource> Do<TSource>(this ObserverBuilder<TSource> source, Action<TSource> onNext, Action onCompleted) =>
        source.OnNext(onNext).OnCompleted(onCompleted);

    public static ObserverBuilder<TSource> Do<TSource>(this ObserverBuilder<TSource> source, Action<TSource> onNext, Action<Exception> onError, Action onCompleted) =>
        source.OnError(onError).OnNext(onNext).OnCompleted(onCompleted);
}