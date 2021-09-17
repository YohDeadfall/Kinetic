using System;

namespace Kinetic.Linq
{
    public static partial class Observable
    {
        public static CountBuilder<ObserverBuilder<TSource>, TSource> Count<TSource>(this IObservable<TSource> observable) =>
            observable.ToBuilder().Count<ObserverBuilder<TSource>, TSource>();

        public static CountBuilder<TObservable, TSource> Count<TObservable, TSource>(this TObservable observable)
            where TObservable : struct, IObserverBuilder<TSource> =>
            new(observable);
    }

    public readonly struct CountBuilder<TObservable, TSource> : IObserverBuilder<int>
        where TObservable : struct, IObserverBuilder<TSource>
    {
        private readonly TObservable _observable;

        public CountBuilder(in TObservable observable) => _observable = observable;

        public void Build<TStateMachine, TFactory>(in TStateMachine stateMachine, ref TFactory factory)
            where TStateMachine : struct, IObserverStateMachine<int>
            where TFactory : struct, IObserverFactory
        {
            _observable.Build(
                stateMachine: new CountStateMachine<TStateMachine, TSource>(stateMachine),
                ref factory);
        }

        public void BuildWithFactory<TStateMachine, TFactory>(in TStateMachine stateMachine, ref TFactory factory)
            where TStateMachine : struct, IObserverStateMachineFactory
            where TFactory : struct, IObserverFactory
        {
            stateMachine.Create<int, CountBuilder<TObservable, TSource>, TFactory>(this, ref factory);
        }
    }

    public struct CountStateMachine<TContinuation, TSource> : IObserverStateMachine<TSource>
        where TContinuation : IObserverStateMachine<int>
    {
        private TContinuation _continuation;
        private int _count;

        public CountStateMachine(TContinuation continuation)
        {
            _continuation = continuation;
            _count = 0;
        }

        public void Initialize(IObserverStateMachineBox box) => _continuation.Initialize(box);
        public void Dispose() => _continuation.Dispose();

        public void OnNext(TSource value) => _count += 1;
        public void OnError(Exception error) => _continuation.OnError(error);

        public void OnCompleted()
        {
            _continuation.OnNext(_count);
            _continuation.OnCompleted();
        }
    }
}