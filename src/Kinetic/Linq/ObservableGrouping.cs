using System;

namespace Kinetic.Linq;

public class ObservableGrouping<TKey, TElement> : ObservableView<TElement>
{
    public TKey Key { get; }

    public ObservableGrouping(TKey key, IObservable<ListChange<TElement>> source) :
        base(source) => Key = key;
}