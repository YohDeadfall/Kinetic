using System;
using Kinetic.Linq.StateMachines;

namespace Kinetic.Linq;

public static partial class Observable
{
    public static ObserverBuilder<TSource> Where<TSource>(this ObserverBuilder<TSource> source, Func<TSource, bool> predicate) =>
        source.ContinueWith<WhereStateMachineFactory<TSource>, TSource>(new(predicate));

    public static ObserverBuilder<TSource> Where<TSource>(this IObservable<TSource> source, Func<TSource, bool> predicate) =>
        source.ToBuilder().Where(predicate);

    private readonly struct WhereStateMachineFactory<TSource> : IObserverStateMachineFactory<TSource, TSource>
    {
        private readonly Func<TSource, bool> _predicate;

        public WhereStateMachineFactory(Func<TSource, bool> predicate) => _predicate = predicate;

        public void Create<TContinuation>(in TContinuation continuation, ObserverStateMachine<TSource> source)
            where TContinuation : struct, IObserverStateMachine<TSource>
        {
            source.ContinueWith(new WhereStateMachine<TContinuation, TSource>(continuation, _predicate));
        }
    }

    private struct WhereStateMachine<TContinuation, TSource> : IObserverStateMachine<TSource>
        where TContinuation : struct, IObserverStateMachine<TSource>
    {
        private TContinuation _continuation;
        private Func<TSource, bool> _predicate;

        public WhereStateMachine(TContinuation continuation, Func<TSource, bool> predicate)
        {
            _continuation = continuation;
            _predicate = predicate;
        }

        public void Initialize(ObserverStateMachineBox box) => _continuation.Initialize(box);
        public void Dispose() => _continuation.Dispose();

        public void OnNext(TSource value)
        {
            try
            {
                if (_predicate(value))
                {
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