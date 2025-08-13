using System.Collections.Generic;
using Kinetic.Runtime;

namespace Kinetic.Linq;

internal struct CollectIntoAccumulator<T, TCollection> : IAccumulator<T, TCollection>
    where TCollection : ICollection<T>
{
    private TCollection _collection;

    public CollectIntoAccumulator(TCollection collection) =>
        _collection = collection;

    public bool Accumulate(T value)
    {
        _collection.Add(value);
        return true;
    }

    public void Publish<TStateMachine>(ref TStateMachine stateMachine)
        where TStateMachine : struct, IStateMachine<TCollection>
    {
        stateMachine.OnNext(_collection);
    }
}