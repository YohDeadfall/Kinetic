using System;

namespace Kinetic.Linq;

internal struct TakeWhileFilter<T> : ITransform<T, bool>
{
    private readonly Func<T, bool> _predicate;
    private bool _matches;

    public TakeWhileFilter(Func<T, bool> predicate)
    {
        _predicate = predicate;
        _matches = true;
    }

    public bool Transform(T value) =>
        _matches &= _predicate(value);
}