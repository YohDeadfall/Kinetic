using System;

namespace Kinetic.Linq;

public interface IGrouping<TKey, TElement> : IObservable<TElement>
{
    TKey Key { get; }
}