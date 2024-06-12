using System;
using Kinetic.Linq.StateMachines;

namespace Kinetic.Linq;

public static partial class ObservableView
{
    public static ObserverBuilder<ListChange<TSource>> OnItemRemovedDispose<TSource>(this ObserverBuilder<ListChange<TSource>> source)
        where TSource : IDisposable =>
        source.OnItemRemoved(item => item?.Dispose());

    public static ObserverBuilder<ListChange<TSource>> OnItemRemovedDispose<TSource>(this ReadOnlyObservableList<TSource> source)
        where TSource : IDisposable =>
        source.Changed.ToBuilder().OnItemRemovedDispose();
}