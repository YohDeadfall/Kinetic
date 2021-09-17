using System;

namespace Kinetic.Linq
{
    public static partial class Observable
    {
        public static SelectBuilder<ObserverBuilder<TSource>, TSource, TResult> Select<TSource, TResult>(this IObservable<TSource> observable, Func<TSource, TResult> selector) =>
            observable.ToBuilder().Select(selector);

        public static SelectBuilder<TObservable, TSource, TResult> Select<TObservable, TSource, TResult>(this TObservable observable, Func<TSource, TResult> selector)
            where TObservable : struct, IObserverBuilder<TSource> =>
            new(observable, selector);
    }

    public readonly struct SelectBuilder<TObservable, TSource, TResult> : IObserverBuilder<TResult>
        where TObservable : struct, IObserverBuilder<TSource>
    {
        private readonly TObservable _observable;
        private readonly Func<TSource, TResult> _selector;

        public SelectBuilder(in TObservable observable, Func<TSource, TResult> selector)
        {
            _observable = observable;
            _selector = selector;
        }

        public void Build<TStateMachine, TFactory>(in TStateMachine stateMachine, ref TFactory factory)
            where TStateMachine : struct, IObserverStateMachine<TResult>
            where TFactory : struct, IObserverFactory
        {
            _observable.Build(
                stateMachine: new SelectStateMachine<TStateMachine, TSource, TResult>(stateMachine, _selector),
                ref factory);
        }

        public void BuildWithFactory<TStateMachine, TFactory>(in TStateMachine stateMachine, ref TFactory factory)
            where TStateMachine : struct, IObserverStateMachineFactory
            where TFactory : struct, IObserverFactory
        {
            stateMachine.Create<TResult, SelectBuilder<TObservable, TSource, TResult>, TFactory>(this, ref factory);
        }
    }

    public struct SelectStateMachine<TContinuation, TSource, TResult> : IObserverStateMachine<TSource>
        where TContinuation : IObserverStateMachine<TResult>
    {
        private TContinuation _continuation;
        private Func<TSource, TResult> _selector;

        public SelectStateMachine(TContinuation continuation, Func<TSource, TResult> selector)
        {
            _continuation = continuation;
            _selector = selector;
        }

        public void Initialize(IObserverStateMachineBox box) => _continuation.Initialize(box);
        public void Dispose() => _continuation.Dispose();

        public void OnNext(TSource value)
        {
            try
            {
                _continuation.OnNext(_selector(value));
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