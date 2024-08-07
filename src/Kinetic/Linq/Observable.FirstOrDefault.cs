using System;
using Kinetic.Linq.StateMachines;

namespace Kinetic.Linq;

public static partial class Observable
{
    public static ObserverBuilder<TSource?> FirstOrDefault<TSource>(this ObserverBuilder<TSource> source) =>
        source.ContinueWith<FirstOrDefaultStateMachineFactory<TSource>, TSource?>(default);

    public static ObserverBuilder<TSource?> FirstOrDefault<TSource>(this ObserverBuilder<TSource> source, Func<TSource, bool> predicate) =>
        source.Where(predicate).FirstOrDefault();

    public static ObserverBuilder<TSource?> FirstOrDefault<TSource>(this IObservable<TSource> source) =>
        source.ToBuilder().FirstOrDefault();

    public static ObserverBuilder<TSource?> FirstOrDefault<TSource>(this IObservable<TSource> source, Func<TSource, bool> predicate) =>
        source.ToBuilder().FirstOrDefault(predicate);

    private readonly struct FirstOrDefaultStateMachineFactory<TSource> : IStateMachineFactory<TSource, TSource?>
    {
        public void Create<TContinuation>(in TContinuation continuation, ObserverStateMachine<TSource> source)
            where TContinuation : struct, IStateMachine<TSource?>
        {
            source.ContinueWith(new FirstOrDefaultStateMachine<TSource, TContinuation>(continuation));
        }
    }

    private struct FirstOrDefaultStateMachine<TSource, TContinuation> : IStateMachine<TSource>
        where TContinuation : struct, IStateMachine<TSource?>
    {
        private TContinuation _continuation;

        public FirstOrDefaultStateMachine(TContinuation continuation) =>
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
            _continuation.OnNext(value);
            _continuation.OnCompleted();
        }

        public void OnError(Exception error)
        {
            _continuation.OnError(error);
        }

        public void OnCompleted()
        {
            _continuation.OnNext(default);
            _continuation.OnCompleted();
        }
    }
}