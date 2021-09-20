using System;
using System.Collections.Generic;

namespace Kinetic.Linq
{
    public static partial class Observable
    {
        public static DistinctBuilder<ObserverBuilder<TSource>, TSource> Distinct<TSource>(this IObservable<TSource> observable, IEqualityComparer<TSource>? comparer = null) =>
            observable.ToBuilder().Distinct<ObserverBuilder<TSource>, TSource>(comparer);

        public static DistinctBuilder<TObservable, TSource> Distinct<TObservable, TSource>(this TObservable observable, IEqualityComparer<TSource>? comparer = null)
            where TObservable : struct, IObserverBuilder<TSource> =>
            new(observable, comparer);
    }

    public readonly struct DistinctBuilder<TObservable, TSource> : IObserverBuilder<TSource>
        where TObservable : struct, IObserverBuilder<TSource>
    {
        private readonly TObservable _observable;
        private readonly IEqualityComparer<TSource>? _comparer;

        public DistinctBuilder(in TObservable observable, IEqualityComparer<TSource>? comparer)
        {
            _observable = observable;
            _comparer = comparer;
        }

        public void Build<TStateMachine, TFactory>(in TStateMachine stateMachine, ref TFactory factory)
            where TStateMachine : struct, IObserverStateMachine<TSource>
            where TFactory : struct, IObserverFactory
        {
            _observable.Build(
                stateMachine: new DistinctStateMachine<TStateMachine, TSource>(stateMachine, _comparer),
                ref factory);
        }

        public void BuildWithFactory<TStateMachine, TFactory>(in TStateMachine stateMachine, ref TFactory factory)
            where TStateMachine : struct, IObserverStateMachineFactory
            where TFactory : struct, IObserverFactory
        {
            stateMachine.Create<TSource, DistinctBuilder<TObservable, TSource>, TFactory>(this, ref factory);
        }
    }

    public struct DistinctStateMachine<TContinuation, TSource> : IObserverStateMachine<TSource>
        where TContinuation : IObserverStateMachine<TSource>
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