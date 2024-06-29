using System;
using System.Collections.Generic;
using Kinetic.Linq.StateMachines;

namespace Kinetic.Linq;

public static partial class Observable
{
    public static ObserverBuilder<List<TSource>> ToList<TSource>(this ObserverBuilder<TSource> source) =>
        source.ContinueWith<ToListStateMachineFactory<TSource>, List<TSource>>(default);

    public static ObserverBuilder<List<TSource>> ToList<TSource>(this IObservable<TSource> source) =>
        source.ToBuilder().ToList();

    private readonly struct ToListStateMachineFactory<TSource> : IStateMachineFactory<TSource, List<TSource>>
    {
        public void Create<TContinuation>(in TContinuation continuation, ObserverStateMachine<TSource> source)
            where TContinuation : struct, IStateMachine<List<TSource>>
        {
            source.ContinueWith(new ToListStateMachine<TSource, TContinuation>(continuation));
        }
    }

    private struct ToListStateMachine<TSource, TContinuation> : IStateMachine<TSource>
        where TContinuation : struct, IStateMachine<List<TSource>>
    {
        private TContinuation _continuation;
        private List<TSource> _result;

        public ToListStateMachine(TContinuation continuation)
        {
            _continuation = continuation;
            _result = new List<TSource>();
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
            try
            {
                _result.Add(value);
            }
            catch (Exception error)
            {
                _continuation.OnError(error);
            }
        }

        public void OnError(Exception error) =>
            _continuation.OnError(error);

        public void OnCompleted()
        {
            _continuation.OnNext(_result);
            _continuation.OnCompleted();
        }
    }
}