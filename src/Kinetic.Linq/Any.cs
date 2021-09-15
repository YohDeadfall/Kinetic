using System;

namespace Kinetic.Linq
{
    public static partial class Observable
    {
        public static AnyBuilder<ObserverBuilder<TSource>, TSource> Any<TSource>(this IObservable<TSource> observable) =>
            observable.ToBuilder().Any<ObserverBuilder<TSource>, TSource>();

        public static AnyBuilder<TObservable, TSource> Any<TObservable, TSource>(this TObservable observable)
            where TObservable : struct, IObserverBuilder<TSource> =>
            new(observable);

        public static AnyPredicateBuilder<ObserverBuilder<TSource>, TSource> Any<TSource>(this IObservable<TSource> observable, Func<TSource, bool> predicate) =>
            observable.ToBuilder().Any(predicate);

        public static AnyPredicateBuilder<TObservable, TSource> Any<TObservable, TSource>(this TObservable observable, Func<TSource, bool> predicate)
            where TObservable : struct, IObserverBuilder<TSource> =>
            new(observable, predicate);
    }

    public readonly struct AnyBuilder<TObservable, TSource> : IObserverBuilder<bool>
        where TObservable : struct, IObserverBuilder<TSource>
    {
        private readonly TObservable _observable;

        public AnyBuilder(in TObservable observable) => _observable = observable;
        public void Build<TStateMachine, TFactory>(in TStateMachine stateMachine, ref TFactory factory)
            where TStateMachine : struct, IObserverStateMachine<bool>
            where TFactory : struct, IObserverFactory
        {
            var any = new AnyStateMachine<TStateMachine, TSource>(stateMachine);
            _observable.Build(any, ref factory);
        }
    }

    public readonly struct AnyPredicateBuilder<TObservable, TSource> : IObserverBuilder<bool>
        where TObservable : struct, IObserverBuilder<TSource>
    {
        private readonly TObservable _observable;
        private readonly Func<TSource, bool> _predicate;

        public AnyPredicateBuilder(in TObservable observable, Func<TSource, bool> predicate)
        {
            _observable = observable;
            _predicate = predicate;
        }

        public void Build<TStateMachine, TFactory>(in TStateMachine stateMachine, ref TFactory factory)
            where TStateMachine : struct, IObserverStateMachine<bool>
            where TFactory : struct, IObserverFactory
        {
            var any = new AnyStateMachine<TStateMachine, TSource>(stateMachine);
            var where = new WhereStateMachine<AnyStateMachine<TStateMachine, TSource>, TSource>(any, _predicate);

            _observable.Build(where, ref factory);
        }
    }

    public struct AnyStateMachine<TContinuation, TSource> : IObserverStateMachine<TSource>
        where TContinuation : IObserverStateMachine<bool>
    {
        private TContinuation _continuation;

        public AnyStateMachine(in TContinuation continuation) => _continuation = continuation;
        public void Initialize(IObserverStateMachineBox box) => _continuation.Initialize(box);
        public void Dispose() => _continuation.Dispose();

        public void OnNext(TSource value)
        {
            _continuation.OnNext(true);
            _continuation.OnCompleted();
        }

        public void OnError(Exception error)
        {
            _continuation.OnError(error);
        }

        public void OnCompleted()
        {
            _continuation.OnNext(false);
            _continuation.OnCompleted();
        }
    }
}