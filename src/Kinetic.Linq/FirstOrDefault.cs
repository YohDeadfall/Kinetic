using System;
using System.Collections.Generic;

namespace Kinetic.Linq
{
    public static partial class Observable
    {
        public static FirstOrDefaultBuilder<ObserverBuilder<TSource>, TSource> FirstOrDefault<TSource>(this IObservable<TSource> observable) =>
            observable.ToBuilder().FirstOrDefault<ObserverBuilder<TSource>, TSource>();

        public static FirstOrDefaultBuilder<TObservable, TSource> FirstOrDefault<TObservable, TSource>(this TObservable observable)
            where TObservable : struct, IObserverBuilder<TSource> =>
            new(observable);
    }

    public readonly struct FirstOrDefaultBuilder<TObservable, TSource> : IObserverBuilder<TSource?>
        where TObservable : struct, IObserverBuilder<TSource>
    {
        private readonly TObservable _observable;

        public FirstOrDefaultBuilder(in TObservable observable)
        {
            _observable = observable;
        }

        public void Build<TStateMachine, TFactory>(in TStateMachine stateMachine, ref TFactory factory)
            where TStateMachine : struct, IObserverStateMachine<TSource?>
            where TFactory : struct, IObserverFactory
        {
            _observable.Build(
                stateMachine: new FirstOrDefaultStateMachine<TStateMachine, TSource>(stateMachine),
                ref factory);
        }

        public void BuildWithFactory<TStateMachine, TFactory>(in TStateMachine stateMachine, ref TFactory factory)
            where TStateMachine : struct, IObserverStateMachineFactory
            where TFactory : struct, IObserverFactory
        {
            stateMachine.Create<TSource?, FirstOrDefaultBuilder<TObservable, TSource>, TFactory>(this, ref factory);
        }
    }

    public struct FirstOrDefaultStateMachine<TContinuation, TSource> : IObserverStateMachine<TSource>
        where TContinuation : IObserverStateMachine<TSource?>
    {
        private TContinuation _continuation;

        public FirstOrDefaultStateMachine(TContinuation continuation) => _continuation = continuation;

        public void Initialize(IObserverStateMachineBox box) => _continuation.Initialize(box);
        public void Dispose() => _continuation.Dispose();

        public void OnNext(TSource value)
        {
            _continuation.OnNext(value);
            _continuation.OnCompleted();
        }

        public void OnError(Exception error)
        {
            _continuation.OnError(error);
        }

        public void OnCompleted()
        {
            _continuation.OnNext(default);
            _continuation.OnCompleted();
        }
    }
}