using System;
using Kinetic.Linq.StateMachines;

namespace Kinetic.Linq;

public static partial class Observable
{
    public static ObserverBuilder<int> Count<TSource>(this ObserverBuilder<TSource> source) =>
        source.ContinueWith<CountStateMachineFactory<TSource>, int>(default);

    public static ObserverBuilder<int> Count<TSource>(this ObserverBuilder<TSource> source, Func<TSource, bool> predicate) =>
        source.Where(predicate).Count();

    public static ObserverBuilder<int> Count<TSource>(this IObservable<TSource> source) =>
        source.ToBuilder().Count();

    public static ObserverBuilder<int> Count<TSource>(this IObservable<TSource> source, Func<TSource, bool> predicate) =>
        source.ToBuilder().Count(predicate);

    private readonly struct CountStateMachineFactory<TSource> : IObserverStateMachineFactory<TSource, int>
    {
        public void Create<TContinuation>(in TContinuation continuation, ObserverStateMachine<TSource> source)
            where TContinuation : struct, IObserverStateMachine<int>
        {
            source.ContinueWith(new CountStateMachine<TContinuation, TSource>(continuation));
        }
    }

    private struct CountStateMachine<TContinuation, TSource> : IObserverStateMachine<TSource>
        where TContinuation : struct, IObserverStateMachine<int>
    {
        private TContinuation _continuation;
        private int _count;

        public CountStateMachine(TContinuation continuation)
        {
            _continuation = continuation;
            _count = 0;
        }

        public void Initialize(ObserverStateMachineBox box) => _continuation.Initialize(box);
        public void Dispose() => _continuation.Dispose();

        public void OnNext(TSource value) => _count += 1;
        public void OnError(Exception error) => _continuation.OnError(error);

        public void OnCompleted()
        {
            _continuation.OnNext(_count);
            _continuation.OnCompleted();
        }
    }
}