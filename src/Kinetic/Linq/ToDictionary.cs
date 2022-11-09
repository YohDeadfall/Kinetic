using System;
using System.Collections.Generic;
using Kinetic.Linq.StateMachines;

namespace Kinetic.Linq;

public static partial class Observable
{
    public static ObserverBuilder<Dictionary<TKey, TSource>> ToDictionary<TSource, TKey>(
        this in ObserverBuilder<TSource> source,
        Func<TSource, TKey> keySelector,
        IEqualityComparer<TKey>? comparer = null)
        where TKey : notnull
    {
        return source.ToDictionary(keySelector, valueSelector: static value => value, comparer);
    }

    public static ObserverBuilder<Dictionary<TKey, TValue>> ToDictionary<TSource, TKey, TValue>(
        this in ObserverBuilder<TSource> source,
        Func<TSource, TKey> keySelector,
        Func<TSource, TValue> valueSelector,
        IEqualityComparer<TKey>? comparer = null)
        where TKey : notnull
    {
        return source.ContinueWith<ToDictionaryStateMachineFactory<TSource, TKey, TValue>, Dictionary<TKey, TValue>>(
            new(keySelector, valueSelector, comparer));
    }

    public static ObserverBuilder<Dictionary<TKey, TSource>> ToDictionary<TSource, TKey>(
        this IObservable<TSource> source,
        Func<TSource, TKey> keySelector,
        IEqualityComparer<TKey>? comparer = null)
        where TKey : notnull
    {
        return source.ToDictionary(keySelector, valueSelector: static value => value, comparer);
    }

    public static ObserverBuilder<Dictionary<TKey, TValue>> ToDictionary<TSource, TKey, TValue>(
        this IObservable<TSource> source,
        Func<TSource, TKey> keySelector,
        Func<TSource, TValue> valueSelector,
        IEqualityComparer<TKey>? comparer = null)
        where TKey : notnull
    {
        return source.ToBuilder().ToDictionary(keySelector, valueSelector, comparer);
    }

    private readonly struct ToDictionaryStateMachineFactory<TSource, TKey, TValue> : IObserverStateMachineFactory<TSource, Dictionary<TKey, TValue>>
        where TKey : notnull
    {
        private readonly Func<TSource, TKey> _keySelector;
        private readonly Func<TSource, TValue> _valueSelector;
        private readonly IEqualityComparer<TKey>? _comparer;

        public ToDictionaryStateMachineFactory(Func<TSource, TKey> keySelector, Func<TSource, TValue> valueSelector, IEqualityComparer<TKey>? comparer)
        {
            _keySelector = keySelector;
            _valueSelector = valueSelector;
            _comparer = comparer;
        }

        public void Create<TContinuation>(in TContinuation continuation, ObserverStateMachine<TSource> source)
            where TContinuation : struct, IObserverStateMachine<Dictionary<TKey, TValue>>
        {
            source.ContinueWith(new ToDictionaryStateMachine<TContinuation, TSource, TKey, TValue>(continuation, _keySelector, _valueSelector, _comparer));
        }
    }

    private struct ToDictionaryStateMachine<TContinuation, TSource, TKey, TValue> : IObserverStateMachine<TSource>
        where TContinuation : struct, IObserverStateMachine<Dictionary<TKey, TValue>>
        where TKey : notnull
    {
        private TContinuation _continuation;
        private Dictionary<TKey, TValue> _result;
        private readonly Func<TSource, TKey> _keySelector;
        private readonly Func<TSource, TValue> _valueSelector;

        public ToDictionaryStateMachine(TContinuation continuation, Func<TSource, TKey> keySelector, Func<TSource, TValue> valueSelector, IEqualityComparer<TKey>? comparer)
        {
            _continuation = continuation;
            _result = new Dictionary<TKey, TValue>(comparer);
            _keySelector = keySelector;
            _valueSelector = valueSelector;
        }

        public void Initialize(ObserverStateMachineBox box) => _continuation.Initialize(box);
        public void Dispose() => _continuation.Dispose();

        public void OnNext(TSource value)
        {
            try
            {
                _result.Add(
                    _keySelector(value),
                    _valueSelector(value));
            }
            catch (Exception error)
            {
                _continuation.OnError(error);
            }
        }

        public void OnError(Exception error)
        {
            _continuation.OnError(error);
        }

        public void OnCompleted()
        {
            _continuation.OnNext(_result);
            _continuation.OnCompleted();
        }
    }
}