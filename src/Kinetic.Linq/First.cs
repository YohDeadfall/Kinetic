using System;
using System.Collections.Generic;
using Kinetic.Linq.StateMachines;

namespace Kinetic.Linq
{
    public static partial class Observable
    {
        public static ObserverBuilder<TSource> First<TSource>(this in ObserverBuilder<TSource> source) =>
            source.ContinueWith<FirstStateMachineFactory<TSource>, TSource>(default);

        public static ObserverBuilder<TSource> First<TSource>(this in ObserverBuilder<TSource> source, Func<TSource, bool> predicate) =>
            source.Where(predicate).First();

        public static ObserverBuilder<TSource> First<TSource>(this IObservable<TSource> source) =>
            source.ToBuilder().First();

        public static ObserverBuilder<TSource> First<TSource>(this IObservable<TSource> source, Func<TSource, bool> predicate) =>
            source.ToBuilder().First(predicate);

        private readonly struct FirstStateMachineFactory<TSource> : IObserverStateMachineFactory<TSource, TSource>
        {
            public void Create<TContinuation>(in TContinuation continuation, ObserverStateMachine<TSource> source)
                where TContinuation : struct, IObserverStateMachine<TSource>
            {
                source.ContinueWith(new FirstStateMachine<TContinuation, TSource>(continuation));
            }
        }

        private struct FirstStateMachine<TContinuation, TSource> : IObserverStateMachine<TSource>
            where TContinuation : IObserverStateMachine<TSource>
        {
            private TContinuation _continuation;

            public FirstStateMachine(TContinuation continuation) => _continuation = continuation;

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
                _continuation.OnError(new InvalidOperationException());
            }
        }
    }
}