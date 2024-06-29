using System;
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

    private readonly struct FirstStateMachineFactory<TSource> : IStateMachineFactory<TSource, TSource>
    {
        public void Create<TContinuation>(in TContinuation continuation, ObserverStateMachine<TSource> source)
            where TContinuation : struct, IStateMachine<TSource>
        {
            source.ContinueWith(new FirstStateMachine<TSource, TContinuation>(continuation));
        }
    }

    private struct FirstStateMachine<TSource, TContinuation> : IStateMachine<TSource>
        where TContinuation : struct, IStateMachine<TSource>
    {
        private TContinuation _continuation;
        private bool _notCompleted;

        public FirstStateMachine(TContinuation continuation)
        {
            _continuation = continuation;
            _notCompleted = true;
        }

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