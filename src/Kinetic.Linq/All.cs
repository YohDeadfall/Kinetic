using System;

namespace Kinetic.Linq
{
    public static partial class Observable
    {
        public static AllBuilder<ObserverBuilder<bool>> All(this IObservable<bool> observable) =>
            observable.ToBuilder().All();

        public static AllBuilder<TObservable> All<TObservable>(this TObservable observable)
            where TObservable : struct, IObserverBuilder<bool> =>
            new(observable);

        public static AllBuilder<ObserverBuilder<TSource>, TSource> All<TSource>(this IObservable<TSource> observable, Func<TSource, bool> predicate) =>
            observable.ToBuilder().All(predicate);

        public static AllBuilder<TObservable, TSource> All<TObservable, TSource>(this TObservable observable, Func<TSource, bool> predicate)
            where TObservable : struct, IObserverBuilder<TSource> =>
            new(observable, predicate);
    }

    public readonly struct AllBuilder<TObservable> : IObserverBuilder<bool>
        where TObservable : struct, IObserverBuilder<bool>
    {
        private readonly TObservable _observable;

        public AllBuilder(in TObservable observable) => _observable = observable;

        public void Build<TStateMachine, TFactory>(in TStateMachine stateMachine, ref TFactory factory)
            where TStateMachine : struct, IObserverStateMachine<bool>
            where TFactory : struct, IObserverFactory
        {
            var observer = new AllStateMachine<TStateMachine>(stateMachine);
            _observable.Build(observer, ref factory);
        }

        public void BuildWithFactory<TStateMachine, TFactory>(in TStateMachine stateMachine, ref TFactory factory)
            where TStateMachine : struct, IObserverStateMachineFactory
            where TFactory : struct, IObserverFactory
        {
            stateMachine.Create<bool, AllBuilder<TObservable>, TFactory>(this, ref factory);
        }
    }

    public readonly struct AllBuilder<TObservable, TSource> : IObserverBuilder<bool>
        where TObservable : struct, IObserverBuilder<TSource>
    {
        private readonly TObservable _observable;
        private readonly Func<TSource, bool> _predicate;

        public AllBuilder(in TObservable observable, Func<TSource, bool> predicate)
        {
            _observable = observable;
            _predicate = predicate;
        }

        public void Build<TStateMachine, TFactory>(in TStateMachine stateMachine, ref TFactory factory)
            where TStateMachine : struct, IObserverStateMachine<bool>
            where TFactory : struct, IObserverFactory
        {
            var all = new AllStateMachine<TStateMachine>(stateMachine);
            var select = new SelectStateMachine<AllStateMachine<TStateMachine>, TSource, bool>(all, _predicate);

            _observable.Build(select, ref factory);
        }

        public void BuildWithFactory<TStateMachine, TFactory>(in TStateMachine stateMachine, ref TFactory factory)
            where TStateMachine : struct, IObserverStateMachineFactory
            where TFactory : struct, IObserverFactory
        {
            stateMachine.Create<bool, AllBuilder<TObservable, TSource>, TFactory>(this, ref factory);
        }
    }

    public struct AllStateMachine<TContinuation> : IObserverStateMachine<bool>
        where TContinuation : IObserverStateMachine<bool>
    {
        private TContinuation _continuation;

        public AllStateMachine(in TContinuation continuation) => _continuation = continuation;
        public void Initialize(IObserverStateMachineBox box) => _continuation.Initialize(box);
        public void Dispose() => _continuation.Dispose();

        public void OnNext(bool value)
        {
            if (!value)
            {
                _continuation.OnNext(false);
                _continuation.OnCompleted();
            }
        }

        public void OnError(Exception error)
        {
            _continuation.OnError(error);
        }

        public void OnCompleted()
        {
            _continuation.OnNext(true);
            _continuation.OnCompleted();
        }
    }
}