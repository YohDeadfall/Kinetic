using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Kinetic.Linq.StateMachines;

namespace Kinetic.Linq
{
    public static partial class Observable
    {
        public static ObserverBuilder<TSource> Last<TSource>(this in ObserverBuilder<TSource> source) =>
            source.ContinueWith<LastStateMachineFactory<TSource>, TSource>(default);

        public static ObserverBuilder<TSource> Last<TSource>(this in ObserverBuilder<TSource> source, Func<TSource, bool> predicate) =>
            source.Where(predicate).Last();

        public static ObserverBuilder<TSource> Last<TSource>(this IObservable<TSource> source) =>
            source.ToBuilder().Last();

        public static ObserverBuilder<TSource> Last<TSource>(this IObservable<TSource> source, Func<TSource, bool> predicate) =>
            source.ToBuilder().Last(predicate);
    }

    internal readonly struct LastStateMachineFactory<TSource> : IObserverStateMachineFactory<TSource, TSource>
    {
        public void Create<TContinuation>(in TContinuation continuation, ObserverStateMachine<TSource> source)
            where TContinuation : struct, IObserverStateMachine<TSource>
        {
            source.ContinueWith(new LastStateMachine<TContinuation, TSource>(continuation));
        }
    }

    internal struct LastStateMachine<TContinuation, TSource> : IObserverStateMachine<TSource>
        where TContinuation : IObserverStateMachine<TSource>
    {
        private TContinuation _continuation;

        [AllowNull]
        private TSource _last;
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
                _continuation.OnNext(_last);
                _continuation.OnCompleted();
            }
            else
            {
                _continuation.OnError(new InvalidOperationException());
            }
        }
    }
}