using System;
using System.Diagnostics.CodeAnalysis;
using Kinetic.Linq.StateMachines;

namespace Kinetic.Linq;

public static partial class Observable
{
    public static ObserverBuilder<TSource> SingleOrDefault<TSource>(this ObserverBuilder<TSource> source) =>
        source.ContinueWith<SingleOrDefaultStateMachineFactory<TSource>, TSource>(default);

    public static ObserverBuilder<TSource> SingleOrDefault<TSource>(this ObserverBuilder<TSource> source, Func<TSource, bool> predicate) =>
        source.Where(predicate).SingleOrDefault();

    public static ObserverBuilder<TSource> SingleOrDefault<TSource>(this IObservable<TSource> source) =>
        source.ToBuilder().SingleOrDefault();

    public static ObserverBuilder<TSource> SingleOrDefault<TSource>(this IObservable<TSource> source, Func<TSource, bool> predicate) =>
        source.ToBuilder().SingleOrDefault(predicate);

    private readonly struct SingleOrDefaultStateMachineFactory<TSource> : IStateMachineFactory<TSource, TSource>
    {
        public void Create<TContinuation>(in TContinuation continuation, ObserverStateMachine<TSource> source)
            where TContinuation : struct, IStateMachine<TSource>
        {
            source.ContinueWith(new SingleOrDefaultStateMachine<TSource, TContinuation>(continuation));
        }
    }

    private struct SingleOrDefaultStateMachine<TSource, TContinuation> : IStateMachine<TSource>
        where TContinuation : struct, IStateMachine<TSource>
    {
        private TContinuation _continuation;

        [AllowNull]
        private TSource _value;
        private bool _hasValue;

        public SingleOrDefaultStateMachine(TContinuation continuation) =>
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
            _continuation.OnNext(_value);
            _continuation.OnCompleted();
        }
    }
}