using System;
using System.Collections.Generic;
using Kinetic.Linq.StateMachines;

namespace Kinetic.Linq;

public static partial class Observable
{
    public static ObserverBuilder<TSource> Distinct<TSource>(this in ObserverBuilder<TSource> source, IEqualityComparer<TSource>? comparer = null) =>
        source.ContinueWith<DistinctStateMachineFactory<TSource>, TSource>(new(comparer));

    public static ObserverBuilder<TSource> Distinct<TSource>(this IObservable<TSource> source, IEqualityComparer<TSource>? comparer = null) =>
        source.ToBuilder().Distinct(comparer);

    private readonly struct DistinctStateMachineFactory<TSource> : IObserverStateMachineFactory<TSource, TSource>
    {
        private readonly IEqualityComparer<TSource>? _comparer;

        public DistinctStateMachineFactory(IEqualityComparer<TSource>? comparer)
        {
            _comparer = comparer;
        }

        public void Create<TContinuation>(in TContinuation continuation, ObserverStateMachine<TSource> source)
            where TContinuation : struct, IObserverStateMachine<TSource>
        {
            source.ContinueWith(new DistinctStateMachine<TContinuation, TSource>(continuation, _comparer));
        }
    }

    private struct DistinctStateMachine<TContinuation, TSource> : IObserverStateMachine<TSource>
        where TContinuation : struct, IObserverStateMachine<TSource>
    {
        private TContinuation _continuation;
        private HashSet<TSource> _set;

        public DistinctStateMachine(TContinuation continuation, IEqualityComparer<TSource>? comparer)
        {
            _continuation = continuation;
            _set = new HashSet<TSource>(comparer);
        }

        public void Initialize(IObserverStateMachineBox box) => _continuation.Initialize(box);
        public void Dispose() => _continuation.Dispose();

        public void OnNext(TSource value)
        {
            if (_set.Add(value))
            {
                _continuation.OnNext(value);
            }
        }

        public void OnError(Exception error)
        {
            _continuation.OnError(error);
        }

        public void OnCompleted()
        {
            _continuation.OnCompleted();
        }
    }
}