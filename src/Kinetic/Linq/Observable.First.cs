using System;
using System.Collections.Generic;
using Kinetic.Linq.StateMachines;

namespace Kinetic.Linq;

public static partial class Observable
{
    public static ObserverBuilder<TSource> First<TSource>(this ObserverBuilder<TSource> source) =>
        source.ContinueWith<FirstStateMachineFactory<TSource>, TSource>(default);

    public static ObserverBuilder<TSource> First<TSource>(this ObserverBuilder<TSource> source, Func<TSource, bool> predicate) =>
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
        where TContinuation : struct, IObserverStateMachine<TSource>
    {
        private TContinuation _continuation;
        private bool _notCompleted;

        public FirstStateMachine(TContinuation continuation)
        {
            _continuation = continuation;
            _notCompleted = true;
        }

        public void Initialize(ObserverStateMachineBox box) => _continuation.Initialize(box);
        public void Dispose() => _continuation.Dispose();

        public void OnNext(TSource value)
        {
            if (_notCompleted)
            {
                _notCompleted = false;
                _continuation.OnNext(value);
                _continuation.OnCompleted();
            }
        }

        public void OnError(Exception error)
        {
            if (_notCompleted)
            {
                _notCompleted = false;
                _continuation.OnError(error);
            }
        }

        public void OnCompleted()
        {
            if (_notCompleted)
            {
                _notCompleted = false;
                _continuation.OnError(new InvalidOperationException());
            }
        }
    }
}