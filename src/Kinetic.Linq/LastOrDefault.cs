using System;
using System.Collections.Generic;

namespace Kinetic.Linq
{
    public static partial class Observable
    {
        public static LastOrDefaultBuilder<ObserverBuilder<TSource>, TSource> LastOrDefault<TSource>(this IObservable<TSource> observable) =>
            observable.ToBuilder().LastOrDefault<ObserverBuilder<TSource>, TSource>();

        public static LastOrDefaultBuilder<TObservable, TSource> LastOrDefault<TObservable, TSource>(this TObservable observable)
            where TObservable : struct, IObserverBuilder<TSource> =>
            new(observable);
    }

    public readonly struct LastOrDefaultBuilder<TObservable, TSource> : IObserverBuilder<TSource?>
        where TObservable : struct, IObserverBuilder<TSource>
    {
        private readonly TObservable _observable;

        public LastOrDefaultBuilder(in TObservable observable)
        {
            _observable = observable;
        }

        public void Build<TStateMachine, TFactory>(in TStateMachine stateMachine, ref TFactory factory)
            where TStateMachine : struct, IObserverStateMachine<TSource?>
            where TFactory : struct, IObserverFactory
        {
            _observable.Build(
                stateMachine: new LastOrDefaultStateMachine<TStateMachine, TSource>(stateMachine),
                ref factory);
        }

        public void BuildWithFactory<TStateMachine, TFactory>(in TStateMachine stateMachine, ref TFactory factory)
            where TStateMachine : struct, IObserverStateMachineFactory
            where TFactory : struct, IObserverFactory
        {
            stateMachine.Create<TSource?, LastOrDefaultBuilder<TObservable, TSource>, TFactory>(this, ref factory);
        }
    }

    public struct LastOrDefaultStateMachine<TContinuation, TSource> : IObserverStateMachine<TSource>
        where TContinuation : IObserverStateMachine<TSource?>
    {
        private TContinuation _continuation;
        private TSource? _last;

        public LastOrDefaultStateMachine(TContinuation continuation)
        {
            _continuation = continuation;
            _last = default;
        }

        public void Initialize(IObserverStateMachineBox box) => _continuation.Initialize(box);
        public void Dispose() => _continuation.Dispose();

        public void OnNext(TSource value)
        {
            _last = value;
        }

        public void OnError(Exception error)
        {
            _continuation.OnError(error);
        }

        public void OnCompleted()
        {
            _continuation.OnNext(_last);
            _continuation.OnCompleted();
        }
    }
}