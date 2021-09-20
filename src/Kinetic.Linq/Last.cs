using System;
using System.Collections.Generic;

namespace Kinetic.Linq
{
    public static partial class Observable
    {
        public static LastBuilder<ObserverBuilder<TSource>, TSource> Last<TSource>(this IObservable<TSource> observable) =>
            observable.ToBuilder().Last<ObserverBuilder<TSource>, TSource>();

        public static LastBuilder<TObservable, TSource> Last<TObservable, TSource>(this TObservable observable)
            where TObservable : struct, IObserverBuilder<TSource> =>
            new(observable);
    }

    public readonly struct LastBuilder<TObservable, TSource> : IObserverBuilder<TSource?>
        where TObservable : struct, IObserverBuilder<TSource>
    {
        private readonly TObservable _observable;

        public LastBuilder(in TObservable observable)
        {
            _observable = observable;
        }

        public void Build<TStateMachine, TFactory>(in TStateMachine stateMachine, ref TFactory factory)
            where TStateMachine : struct, IObserverStateMachine<TSource?>
            where TFactory : struct, IObserverFactory
        {
            _observable.Build(
                stateMachine: new LastStateMachine<TStateMachine, TSource>(stateMachine),
                ref factory);
        }

        public void BuildWithFactory<TStateMachine, TFactory>(in TStateMachine stateMachine, ref TFactory factory)
            where TStateMachine : struct, IObserverStateMachineFactory
            where TFactory : struct, IObserverFactory
        {
            stateMachine.Create<TSource?, LastBuilder<TObservable, TSource>, TFactory>(this, ref factory);
        }
    }

    public struct LastStateMachine<TContinuation, TSource> : IObserverStateMachine<TSource>
        where TContinuation : IObserverStateMachine<TSource?>
    {
        private TContinuation _continuation;
        private TSource? _last;
        private bool _hasLast;

        public LastStateMachine(TContinuation continuation)
        {
            _continuation = continuation;
            _last = default;
            _hasLast = false;
        }

        public void Initialize(IObserverStateMachineBox box) => _continuation.Initialize(box);
        public void Dispose() => _continuation.Dispose();

        public void OnNext(TSource value)
        {
            _last = value;
            _hasLast = true;
        }

        public void OnError(Exception error)
        {
            _continuation.OnError(error);
        }

        public void OnCompleted()
        {
            if (_hasLast)
            {
                _continuation.OnNext(_last!);
                _continuation.OnCompleted();
            }
            else
            {
                _continuation.OnError(new InvalidOperationException());
            }
        }
    }
}