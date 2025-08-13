using System;

namespace Kinetic.Linq;

internal struct SkipWhileIndexedFilter<T> : ITransform<T, bool>
{
    private readonly Func<T, int, bool> _predicate;
    private bool _matches;
    private int _index;

    public SkipWhileIndexedFilter(Func<T, int, bool> predicate) =>
        _predicate = predicate;

    public bool Transform(T value) =>
        _matches || (_matches = !_predicate(value, checked(_index++)));
}