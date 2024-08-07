using System;
using System.Diagnostics.CodeAnalysis;
using Kinetic.Linq.StateMachines;

namespace Kinetic.Linq;

public static partial class Observable
{
    public static ObserverBuilder<TSource> Last<TSource>(this ObserverBuilder<TSource> source) =>
        source.ContinueWith<LastStateMachineFactory<TSource>, TSource>(default);

    public static ObserverBuilder<TSource> Last<TSource>(this ObserverBuilder<TSource> source, Func<TSource, bool> predicate) =>
        source.Where(predicate).Last();

    public static ObserverBuilder<TSource> Last<TSource>(this IObservable<TSource> source) =>
        source.ToBuilder().Last();

    public static ObserverBuilder<TSource> Last<TSource>(this IObservable<TSource> source, Func<TSource, bool> predicate) =>
        source.ToBuilder().Last(predicate);

    private readonly struct LastStateMachineFactory<TSource> : IStateMachineFactory<TSource, TSource>
    {
        public void Create<TContinuation>(in TContinuation continuation, ObserverStateMachine<TSource> source)
            where TContinuation : struct, IStateMachine<TSource>
        {
            source.ContinueWith(new LastStateMachine<TSource, TContinuation>(continuation));
        }
    }

    private struct LastStateMachine<TSource, TContinuation> : IStateMachine<TSource>
        where TContinuation : struct, IStateMachine<TSource>
    {
        private TContinuation _continuation;

        [AllowNull]
        private TSource _last;
        private bool _hasLast;

        public LastStateMachine(TContinuation continuation) =>
            _continuation = continuation;

        public StateMachineBox Box =>
            _continuation.Box;

        public StateMachine<TSource> Reference =>
            StateMachine<TSource>.Create(ref this);

        public StateMachine? Continuation =>
            _continuation.Reference;

        public void Initialize(StateMachineBox box) =>
            _continuation.Initialize(box);

        public void Dispose() =>
            _continuation.Dispose();

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