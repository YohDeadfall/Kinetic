using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Kinetic.Linq;

internal struct CompareBy<T, TKey, TOrdering> : IAggregator<T, T>
    where TOrdering : IOrdering
{
    private readonly Func<T, TKey> _keySelector;
    private readonly IComparer<TKey>? _comparer;

    [AllowNull]
    private TKey _resultKey;
    private bool _initialized;

    public CompareBy(Func<T, TKey> keySelector, IComparer<TKey>? comparer)
    {
        _keySelector = keySelector;
        _comparer = comparer;
    }

    public static bool RequiresSeed => true;

    public bool Aggregate(T value, ref T result)
    {
        if (!_initialized)
        {
            _resultKey = _keySelector(result);
            _initialized = true;
        }

        var valueKey = _keySelector(value);
        var ordering = _comparer?.Compare(valueKey, _resultKey)
            ?? Comparer<TKey>.Default.Compare(valueKey, _resultKey);

        if (TOrdering.Matches(ordering))
        {
            result = value;
            _resultKey = valueKey;
        }

        return true;
    }
}
