using System;

namespace Kinetic.Linq
{
    public static partial class Observable
    {
        public static TakeWhileBuilder<ObserverBuilder<TSource>, TSource> TakeWhile<TSource>(this IObservable<TSource> observable, Func<TSource, bool> predicate) =>
            observable.ToBuilder().TakeWhile(predicate);

        public static TakeWhileBuilder<TObservable, TSource> TakeWhile<TObservable, TSource>(this TObservable observable, Func<TSource, bool> predicate)
            where TObservable : struct, IObserverBuilder<TSource> =>
            new(observable, predicate);
    }

    public readonly struct TakeWhileBuilder<TObservable, TSource> : IObserverBuilder<TSource>
        where TObservable : struct, IObserverBuilder<TSource>
    {
        private readonly TObservable _observable;
        private readonly Func<TSource, bool> _predicate;

        public TakeWhileBuilder(in TObservable observable, Func<TSource, bool> predicate)
        {
            _observable = observable;
            _predicate = predicate;
        }

        public void Build<TStateMachine, TFactory>(in TStateMachine stateMachine, ref TFactory factory)
            where TStateMachine : struct, IObserverStateMachine<TSource>
            where TFactory : struct, IObserverFactory
        {
            _observable.Build(
                stateMachine: new TakeWhileStateMachine<TStateMachine, TSource>(stateMachine, _predicate),
                ref factory);
        }
    }

    public struct TakeWhileStateMachine<TContinuation, TSource> : IObserverStateMachine<TSource>
        where TContinuation : IObserverStateMachine<TSource>
    {
        private TContinuation _continuation;
        private Func<TSource, bool> _predicate;

        public TakeWhileStateMachine(TContinuation continuation, Func<TSource, bool> predicate)
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
                else
                {
                    _continuation.OnCompleted();
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