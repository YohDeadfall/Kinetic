using System;
using System.Diagnostics.CodeAnalysis;
using Kinetic.Linq.StateMachines;

namespace Kinetic.Linq;

public static partial class Observable
{
    public static ObserverBuilder<TSource> Single<TSource>(this ObserverBuilder<TSource> source) =>
        source.ContinueWith<SingleStateMachineFactory<TSource>, TSource>(default);

    public static ObserverBuilder<TSource> Single<TSource>(this ObserverBuilder<TSource> source, Func<TSource, bool> predicate) =>
        source.Where(predicate).Single();

    public static ObserverBuilder<TSource> Single<TSource>(this IObservable<TSource> source) =>
        source.ToBuilder().Single();

    public static ObserverBuilder<TSource> Single<TSource>(this IObservable<TSource> source, Func<TSource, bool> predicate) =>
        source.ToBuilder().Single(predicate);

    private readonly struct SingleStateMachineFactory<TSource> : IStateMachineFactory<TSource, TSource>
    {
        public void Create<TContinuation>(in TContinuation continuation, ObserverStateMachine<TSource> source)
            where TContinuation : struct, IStateMachine<TSource>
        {
            source.ContinueWith(new SingleStateMachine<TSource, TContinuation>(continuation));
        }
    }

    private struct SingleStateMachine<TSource, TContinuation> : IStateMachine<TSource>
        where TContinuation : struct, IStateMachine<TSource>
    {
        private TContinuation _continuation;

        [AllowNull]
        private TSource _value;
        private bool _hasValue;

        public SingleStateMachine(TContinuation continuation) =>
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
            if (_hasValue)
            {
                _continuation.OnError(new InvalidOperationException());
            }
            else
            {
                _value = value;
                _hasValue = true;
            }
        }

        public void OnError(Exception error)
        {
            _continuation.OnError(error);
        }

        public void OnCompleted()
        {
            if (_hasValue)
            {
                _continuation.OnNext(_value);
                _continuation.OnCompleted();
            }
            else
            {
                _continuation.OnError(new InvalidOperationException());
            }
        }
    }
}