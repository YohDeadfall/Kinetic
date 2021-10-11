using System;
using Kinetic.Linq.StateMachines;

namespace Kinetic.Linq
{
    public static partial class Observable
    {
        public static ObserverBuilder<TSource> Skip<TSource>(this in ObserverBuilder<TSource> source, int count) =>
            source.ContinueWith<SkipStateMachineFactory<TSource>, TSource>(new(count));

        public static ObserverBuilder<TSource> Skip<TSource>(this IObservable<TSource> source, int count) =>
            source.ToBuilder().Skip(count);

        private readonly struct SkipStateMachineFactory<TSource> : IObserverStateMachineFactory<TSource, TSource>
        {
            private readonly int _count;

            public SkipStateMachineFactory(int count)
            {
                _count = count >= 0 ? count : throw new ArgumentOutOfRangeException(nameof(count));
            }

            public void Create<TContinuation>(in TContinuation continuation, ObserverStateMachine<TSource> source)
                where TContinuation : struct, IObserverStateMachine<TSource>
            {
                source.ContinueWith(new SkipStateMachine<TContinuation, TSource>(continuation, (uint) _count));
            }
        }

        private struct SkipStateMachine<TContinuation, TSource> : IObserverStateMachine<TSource>
            where TContinuation : struct, IObserverStateMachine<TSource>
        {
            private TContinuation _continuation;
            private uint _count;

            public SkipStateMachine(TContinuation continuation, uint count)
            {
                _continuation = continuation;
                _count = count;
            }

            public void Initialize(IObserverStateMachineBox box) => _continuation.Initialize(box);
            public void Dispose() => _continuation.Dispose();

            public void OnNext(TSource value)
            {
                if (_count != 0)
                {
                    _count -= 1;
                }
                else
                {
                    _continuation.OnNext(value);
                }
            }

            public void OnError(Exception error) => _continuation.OnError(error);
            public void OnCompleted() => _continuation.OnCompleted();
        }
    }
}