using System;
using Kinetic.Linq.StateMachines;

namespace Kinetic.Linq;

public static partial class Observable
{
    public static ObserverBuilder<bool> Any<TSource>(this in ObserverBuilder<TSource> source) =>
        source.ContinueWith<AnyStateMachineFactory<TSource>, bool>(default);

    public static ObserverBuilder<bool> Any<TSource>(this in ObserverBuilder<TSource> source, Func<TSource, bool> predicate) =>
        source.Where(predicate).Any();

    public static ObserverBuilder<bool> Any<TSource>(this IObservable<TSource> source) =>
        source.ToBuilder().Any();

    public static ObserverBuilder<bool> Any<TSource>(this IObservable<TSource> source, Func<TSource, bool> predicate) =>
        source.ToBuilder().Any(predicate);

    private readonly struct AnyStateMachineFactory<TSource> : IObserverStateMachineFactory<TSource, bool>
    {
        public void Create<TContinuation>(in TContinuation continuation, ObserverStateMachine<TSource> source)
            where TContinuation : struct, IObserverStateMachine<bool>
        {
            source.ContinueWith(new AnyStateMachine<TContinuation, TSource>(continuation));
        }
    }

    private struct AnyStateMachine<TContinuation, TSource> : IObserverStateMachine<TSource>
        where TContinuation : struct, IObserverStateMachine<bool>
    {
        private TContinuation _continuation;

        public AnyStateMachine(in TContinuation continuation) => _continuation = continuation;
        public void Initialize(IObserverStateMachineBox box) => _continuation.Initialize(box);
        public void Dispose() => _continuation.Dispose();

        public void OnNext(TSource value)
        {
            _continuation.OnNext(true);
            _continuation.OnCompleted();
        }

        public void OnError(Exception error)
        {
            _continuation.OnError(error);
        }

        public void OnCompleted()
        {
            _continuation.OnNext(false);
            _continuation.OnCompleted();
        }
    }
}