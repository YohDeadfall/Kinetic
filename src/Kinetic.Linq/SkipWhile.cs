using System;

namespace Kinetic.Linq
{
    public static partial class Observable
    {
        public static SkipWhileBuilder<ObserverBuilder<TSource>, TSource> SkipWhile<TSource>(this IObservable<TSource> observable, Func<TSource, bool> predicate) =>
            observable.ToBuilder().SkipWhile(predicate);

        public static SkipWhileBuilder<TObservable, TSource> SkipWhile<TObservable, TSource>(this TObservable observable, Func<TSource, bool> predicate)
            where TObservable : struct, IObserverBuilder<TSource> =>
            new(observable, predicate);
    }

    public readonly struct SkipWhileBuilder<TObservable, TSource> : IObserverBuilder<TSource>
        where TObservable : struct, IObserverBuilder<TSource>
    {
        private readonly TObservable _observable;
        private readonly Func<TSource, bool> _predicate;

        public SkipWhileBuilder(in TObservable observable, Func<TSource, bool> predicate)
        {
            _observable = observable;
            _predicate = predicate;
        }

        public void Build<TStateMachine, TFactory>(in TStateMachine stateMachine, ref TFactory factory)
            where TStateMachine : struct, IObserverStateMachine<TSource>
            where TFactory : struct, IObserverFactory
        {
            _observable.Build(
                stateMachine: new SkipWhileStateMachine<TStateMachine, TSource>(stateMachine, _predicate),
                ref factory);
        }

        public void BuildWithFactory<TStateMachine, TFactory>(in TStateMachine stateMachine, ref TFactory factory)
            where TStateMachine : struct, IObserverStateMachineFactory
            where TFactory : struct, IObserverFactory
        {
            stateMachine.Create<TSource, SkipWhileBuilder<TObservable, TSource>, TFactory>(this, ref factory);
        }
    }

    public struct SkipWhileStateMachine<TContinuation, TSource> : IObserverStateMachine<TSource>
        where TContinuation : IObserverStateMachine<TSource>
    {
        private TContinuation _continuation;
        private Func<TSource, bool>? _predicate;

        public SkipWhileStateMachine(TContinuation continuation, Func<TSource, bool> predicate)
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
                if (_predicate?.Invoke(value) != true)
                {
                    _predicate = null;
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