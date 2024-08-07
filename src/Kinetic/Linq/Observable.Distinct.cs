using System;
using System.Collections.Generic;
using Kinetic.Linq.StateMachines;

namespace Kinetic.Linq;

public static partial class Observable
{
    public static ObserverBuilder<TSource> Distinct<TSource>(this ObserverBuilder<TSource> source, IEqualityComparer<TSource>? comparer = null) =>
        source.ContinueWith<DistinctStateMachineFactory<TSource>, TSource>(new(comparer));

    public static ObserverBuilder<TSource> Distinct<TSource>(this IObservable<TSource> source, IEqualityComparer<TSource>? comparer = null) =>
        source.ToBuilder().Distinct(comparer);

    private readonly struct DistinctStateMachineFactory<TSource> : IStateMachineFactory<TSource, TSource>
    {
        private readonly IEqualityComparer<TSource>? _comparer;

        public DistinctStateMachineFactory(IEqualityComparer<TSource>? comparer)
        {
            _comparer = comparer;
        }

        public void Create<TContinuation>(in TContinuation continuation, ObserverStateMachine<TSource> source)
            where TContinuation : struct, IStateMachine<TSource>
        {
            source.ContinueWith(new DistinctStateMachine<TSource, TContinuation>(continuation, _comparer));
        }
    }

    private struct DistinctStateMachine<TSource, TContinuation> : IStateMachine<TSource>
        where TContinuation : struct, IStateMachine<TSource>
    {
        private TContinuation _continuation;
        private readonly HashSet<TSource> _set;

        public DistinctStateMachine(TContinuation continuation, IEqualityComparer<TSource>? comparer)
        {
            _continuation = continuation;
            _set = new HashSet<TSource>(comparer);
        }

        public StateMachineBox Box =>
            _continuation.Box;

        public StateMachine<TSource> Reference =>
            StateMachine<TSource>.Create(ref this);

        public StateMachine? Continuation =>
            _continuation.Reference;

        public void Initialize(StateMachineBox box) =>
            _continuation.Initialize(box);

        public void Dispose() =>
            _continuation.Dispose();

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