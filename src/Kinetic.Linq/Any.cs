using System;

namespace Kinetic.Linq
{
    public static partial class Observable
    {
        public static AnyBuilder<ObserverBuilder<bool>> Any(this IObservable<bool> observable) =>
            observable.ToBuilder().Any();

        public static AnyBuilder<TObservable> Any<TObservable>(this TObservable observable)
            where TObservable : struct, IObserverBuilder<bool> =>
            new(observable);
    }

    public readonly struct AnyBuilder<TObservable> : IObserverBuilder<bool>
        where TObservable : struct, IObserverBuilder<bool>
    {
        private readonly TObservable _observable;

        public AnyBuilder(in TObservable observable) => _observable = observable;
        public void Build<TStateMachine, TFactory>(in TStateMachine stateMachine, ref TFactory factory)
            where TStateMachine : struct, IObserverStateMachine<bool>
            where TFactory : struct, IObserverFactory
        {
            var observer = new AnyStateMachine<TStateMachine>(stateMachine);
            _observable.Build(observer, ref factory);
        }
    }

    public struct AnyStateMachine<TContinuation> : IObserverStateMachine<bool>
        where TContinuation : IObserverStateMachine<bool>
    {
        private TContinuation _continuation;

        public AnyStateMachine(in TContinuation continuation) => _continuation = continuation;
        public void Initialize(IObserverStateMachineBox box) => _continuation.Initialize(box);
        public void Dispose() => _continuation.Dispose();

        public void OnNext(bool value)
        {
            if (value)
            {
                _continuation.OnNext(true);
                _continuation.OnCompleted();
            }
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