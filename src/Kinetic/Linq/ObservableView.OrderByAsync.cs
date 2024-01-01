using System;
using System.Collections.Generic;
using Kinetic.Linq.StateMachines;

namespace Kinetic.Linq;

public static partial class ObservableView
{
    public static ObserverBuilder<ListChange<T>> OrderByAsync<T, TKey>(this ObserverBuilder<ListChange<T>> source, ObserverBuilderFactory<T, TKey> keySelector, IComparer<TKey>? keyComparer = null) =>
        source.ContinueWith<OrderByCore<T, TKey>.StateMachineFactory, ListChange<T>>(new(keySelector, keyComparer));

    public static ObserverBuilder<ListChange<T>> OrderByAsync<T, TKey>(this ObserverBuilder<ListChange<T>> source, Func<T, Property<TKey>> keySelector, IComparer<TKey>? keyComparer = null) =>
        source.OrderByAsync(item => keySelector(item).Changed.ToBuilder(), keyComparer);

    public static ObserverBuilder<ListChange<T>> OrderByAsync<T, TKey>(this ObserverBuilder<ListChange<T>> source, Func<T, ReadOnlyProperty<TKey>> keySelector, IComparer<TKey>? keyComparer = null) =>
        source.OrderByAsync(item => keySelector(item).Changed.ToBuilder(), keyComparer);

    public static ObserverBuilder<ListChange<T>> OrderByAsync<T, TKey>(this ReadOnlyObservableList<T> source, ObserverBuilderFactory<T, TKey> keySelector, IComparer<TKey>? keyComparer = null) =>
        source.Changed.ToBuilder().OrderByAsync(keySelector, keyComparer);

    public static ObserverBuilder<ListChange<T>> OrderByAsync<T, TKey>(this ReadOnlyObservableList<T> source, Func<T, Property<TKey>> keySelector, IComparer<TKey>? keyComparer = null) =>
        source.OrderByAsync(item => keySelector(item).Changed.ToBuilder(), keyComparer);

    public static ObserverBuilder<ListChange<T>> OrderByAsync<T, TKey>(this ReadOnlyObservableList<T> source, Func<T, ReadOnlyProperty<TKey>> keySelector, IComparer<TKey>? keyComparer = null) =>
        source.OrderByAsync(item => keySelector(item).Changed.ToBuilder(), keyComparer);

}