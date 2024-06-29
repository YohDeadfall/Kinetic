using System;
using Kinetic.Linq.StateMachines;

namespace Kinetic.Linq;

public static partial class Observable
{
    public static ObserverBuilder<TSource?> LastOrDefault<TSource>(this ObserverBuilder<TSource> source) =>
        source.ContinueWith<LastOrDefaultStateMachineFactory<TSource>, TSource?>(default);

    public static ObserverBuilder<TSource?> LastOrDefault<TSource>(this ObserverBuilder<TSource> source, Func<TSource, bool> predicate) =>
        source.Where(predicate).LastOrDefault();

    public static ObserverBuilder<TSource?> LastOrDefault<TSource>(this IObservable<TSource> source) =>
        source.ToBuilder().LastOrDefault();

    public static ObserverBuilder<TSource?> LastOrDefault<TSource>(this IObservable<TSource> source, Func<TSource, bool> predicate) =>
        source.ToBuilder().LastOrDefault(predicate);

    private readonly struct LastOrDefaultStateMachineFactory<TSource> : IStateMachineFactory<TSource, TSource?>
    {
        public void Create<TContinuation>(in TContinuation continuation, ObserverStateMachine<TSource> source)
            where TContinuation : struct, IStateMachine<TSource?>
        {
            source.ContinueWith(new LastOrDefaultStateMachine<TSource, TContinuation>(continuation));
        }
    }

    private struct LastOrDefaultStateMachine<TSource, TContinuation> : IStateMachine<TSource>
        where TContinuation : struct, IStateMachine<TSource?>
    {
        private TContinuation _continuation;
        private TSource? _last;

        public LastOrDefaultStateMachine(TContinuation continuation)
        {
            _continuation = continuation;
            _last = default;
        }

        public StateMachineBox Box =>
            _continuation.Box;

        public void Initialize(StateMachineBox box) =>
            _continuation.Initialize(box);

        public void Dispose() =>
            _continuation.Dispose();

        public void OnNext(TSource value) =>
            _last = value;

        public void OnError(Exception error) =>
            _continuation.OnError(error);

        public void OnCompleted()
        {
            _continuation.OnNext(_last);
            _continuation.OnCompleted();
        }
    }
}