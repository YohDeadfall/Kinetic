using System;
using Kinetic.Linq.StateMachines;

namespace Kinetic.Linq;

public static partial class ObservableView
{
    public static ObserverBuilder<ListChange<TSource>> OnItemAdded<TSource>(this ObserverBuilder<ListChange<TSource>> source, Action<TSource> action) =>
        source.Do(change =>
        {
            if (change.Action is ListChangeAction.Insert or ListChangeAction.Replace)
                action(change.NewItem);
        });

    public static ObserverBuilder<ListChange<TSource>> OnItemAdded<TSource>(this ReadOnlyObservableList<TSource> source, Action<TSource> action) =>
        source.Changed.ToBuilder().OnItemAdded(action);
}