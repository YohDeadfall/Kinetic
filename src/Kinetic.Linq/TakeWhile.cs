using System;
using Kinetic.Linq.StateMachines;

namespace Kinetic.Linq
{
    public static partial class Observable
    {
        public static ObserverBuilder<TSource> TakeWhile<TSource>(this in ObserverBuilder<TSource> source, Func<TSource, bool> predicate) =>
            source.ContinueWith<TakeWhileStateMachineFactory<TSource>, TSource>(new(predicate));

        public static ObserverBuilder<TSource> TakeWhile<TSource>(this IObservable<TSource> source, Func<TSource, bool> predicate) =>
            source.ToBuilder().TakeWhile(predicate);

        private readonly struct TakeWhileStateMachineFactory<TSource> : IObserverStateMachineFactory<TSource, TSource>
        {
            private readonly Func<TSource, bool> _predicate;

            public TakeWhileStateMachineFactory(Func<TSource, bool> predicate)
            {
                _predicate = predicate;
            }

            public void Create<TContinuation>(in TContinuation continuation, ObserverStateMachine<TSource> source)
                where TContinuation : struct, IObserverStateMachine<TSource>
            {
                source.ContinueWith(new TakeWhileStateMachine<TContinuation, TSource>(continuation, _predicate));
            }
        }

        private struct TakeWhileStateMachine<TContinuation, TSource> : IObserverStateMachine<TSource>
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
}