using System.Linq;
using Kinetic.Linq.StateMachines;

namespace Kinetic.Linq;

public static class ObservableGrouping
{
    public static ObservableGrouping<TKey, TElement> Create<TKey, TElement>(TKey key, ObserverBuilder<ListChange<TElement>> builder) =>
        new(key, builder);
}

public class ObservableGrouping<TKey, TElement> : ObservableView<TElement>, IGrouping<TKey, TElement>
{
    public TKey Key { get; }

    public ObservableGrouping(TKey key, ObserverBuilder<ListChange<TElement>> builder) :
        base(builder) => Key = key;
}