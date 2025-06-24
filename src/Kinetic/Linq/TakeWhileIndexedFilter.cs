using System;

namespace Kinetic.Linq;

internal struct TakeWhileIndexedFilter<T> : ITransform<T, bool>
{
    private readonly Func<T, int, bool> _predicate;
    private bool _matches;
    private int _index;

    public TakeWhileIndexedFilter(Func<T, int, bool> predicate)
    {
        _predicate = predicate;
        _matches = true;
    }

    public bool Transform(T value) =>
        _matches &= _predicate(value, checked(_index++));
}