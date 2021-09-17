using System;

namespace Kinetic.Linq
{
    public static partial class Observable
    {
        public static WhereBuilder<ObserverBuilder<TSource>, TSource> Where<TSource>(this IObservable<TSource> observable, Func<TSource, bool> predicate) =>
            observable.ToBuilder().Where(predicate);

        public static WhereBuilder<TObservable, TSource> Where<TObservable, TSource>(this TObservable observable, Func<TSource, bool> predicate)
            where TObservable : struct, IObserverBuilder<TSource> =>
            new(observable, predicate);
    }

    public readonly struct WhereBuilder<TObservable, TSource> : IObserverBuilder<TSource>
        where TObservable : struct, IObserverBuilder<TSource>
    {
        private readonly TObservable _observable;
        private readonly Func<TSource, bool> _predicate;

        public WhereBuilder(in TObservable observable, Func<TSource, bool> predicate)
        {
            _observable = observable;
            _predicate = predicate;
        }

        public void Build<TStateMachine, TFactory>(in TStateMachine stateMachine, ref TFactory factory)
            where TStateMachine : struct, IObserverStateMachine<TSource>
            where TFactory : struct, IObserverFactory
        {
            _observable.Build(
                stateMachine: new WhereStateMachine<TStateMachine, TSource>(stateMachine, _predicate),
                ref factory);
        }

        public void BuildWithFactory<TStateMachine, TFactory>(in TStateMachine stateMachine, ref TFactory factory)
            where TStateMachine : struct, IObserverStateMachineFactory
            where TFactory : struct, IObserverFactory
        {
            stateMachine.Create<TSource, WhereBuilder<TObservable, TSource>, TFactory>(this, ref factory);
        }
    }

    public struct WhereStateMachine<TContinuation, TSource> : IObserverStateMachine<TSource>
        where TContinuation : IObserverStateMachine<TSource>
    {
        private TContinuation _continuation;
        private Func<TSource, bool> _predicate;

        public WhereStateMachine(TContinuation continuation, Func<TSource, bool> predicate)
        {
            _continuation = continuation;
            _predicate = predicate;
        }

        public void Initialize(IObserverStateMachineBox box) => _continuation.Initialize(box);
        public void Dispose() => _continuation.Dispose();

        public void OnNext(TSource value)
        {
            try
            {
                if (_predicate(value))
                {
                    _continuation.OnNext(value);
                }
            }
            catch (Exception error)
            {
                _continuation.OnError(error);
            }
        }

        public void OnError(Exception error) => _continuation.OnError(error);
        public void OnCompleted() => _continuation.OnCompleted();
    }
}