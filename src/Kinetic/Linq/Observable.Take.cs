using System;
using Kinetic.Linq.StateMachines;

namespace Kinetic.Linq;

public static partial class Observable
{
    public static ObserverBuilder<TSource> Take<TSource>(this ObserverBuilder<TSource> source, int count) =>
        source.ContinueWith<TakeStateMachineFactory<TSource>, TSource>(new(count));

    public static ObserverBuilder<TSource> Take<TSource>(this IObservable<TSource> source, int count) =>
        source.ToBuilder().Take(count);

    private readonly struct TakeStateMachineFactory<TSource> : IStateMachineFactory<TSource, TSource>
    {
        private readonly int _count;

        public TakeStateMachineFactory(int count)
        {
            _count = count >= 0 ? count : throw new ArgumentOutOfRangeException(nameof(count));
        }

        public void Create<TContinuation>(in TContinuation continuation, ObserverStateMachine<TSource> source)
            where TContinuation : struct, IStateMachine<TSource>
        {
            source.ContinueWith(new TakeStateMachine<TSource, TContinuation>(continuation, (uint) _count));
        }
    }

    private struct TakeStateMachine<TSource, TContinuation> : IStateMachine<TSource>
        where TContinuation : struct, IStateMachine<TSource>
    {
        private TContinuation _continuation;
        private uint _count;

        public TakeStateMachine(TContinuation continuation, uint count)
        {
            _continuation = continuation;
            _count = count;
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
            if (_count != 0)
            {
                _count -= 1;
                _continuation.OnNext(value);
            }
        }

        public void OnError(Exception error) =>
            _continuation.OnError(error);

        public void OnCompleted() =>
            _continuation.OnCompleted();
    }
}