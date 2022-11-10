using System;
using System.Collections.Generic;
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

    private readonly struct FirstOrDefaultStateMachineFactory<TSource> : IObserverStateMachineFactory<TSource, TSource?>
    {
        public void Create<TContinuation>(in TContinuation continuation, ObserverStateMachine<TSource> source)
            where TContinuation : struct, IObserverStateMachine<TSource?>
        {
            source.ContinueWith(new FirstOrDefaultStateMachine<TContinuation, TSource>(continuation));
        }
    }

    private struct FirstOrDefaultStateMachine<TContinuation, TSource> : IObserverStateMachine<TSource>
        where TContinuation : struct, IObserverStateMachine<TSource?>
    {
        private TContinuation _continuation;

        public FirstOrDefaultStateMachine(TContinuation continuation) => _continuation = continuation;

        public void Initialize(ObserverStateMachineBox box) => _continuation.Initialize(box);
        public void Dispose() => _continuation.Dispose();

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