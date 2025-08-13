using System;

namespace Kinetic.Linq;

internal struct SkipWhileFilter<T> : ITransform<T, bool>
{
    private readonly Func<T, bool> _predicate;
    private bool _matches;

    public SkipWhileFilter(Func<T, bool> predicate) =>
        _predicate = predicate;

    public bool Transform(T value) =>
        _matches || (_matches = !_predicate(value));
}