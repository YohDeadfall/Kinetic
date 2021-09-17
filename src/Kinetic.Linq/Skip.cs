using System;

namespace Kinetic.Linq
{
    public static partial class Observable
    {
        public static SkipBuilder<ObserverBuilder<TSource>, TSource> Skip<TSource>(this IObservable<TSource> observable, int count) =>
            observable.ToBuilder().Skip<ObserverBuilder<TSource>, TSource>(count);

        public static SkipBuilder<TObservable, TSource> Skip<TObservable, TSource>(this TObservable observable, int count)
            where TObservable : struct, IObserverBuilder<TSource> =>
            new(observable, count);
    }

    public readonly struct SkipBuilder<TObservable, TSource> : IObserverBuilder<TSource>
        where TObservable : struct, IObserverBuilder<TSource>
    {
        private readonly TObservable _observable;
        private readonly int _count;

        public SkipBuilder(in TObservable observable, int count)
        {
            _observable = observable;
            _count = count >= 0 ? count : throw new ArgumentOutOfRangeException(nameof(count));
        }

        public void Build<TStateMachine, TFactory>(in TStateMachine stateMachine, ref TFactory factory)
            where TStateMachine : struct, IObserverStateMachine<TSource>
            where TFactory : struct, IObserverFactory
        {
            _observable.Build(
                stateMachine: new SkipStateMachine<TStateMachine, TSource>(stateMachine, (uint) _count),
                ref factory);
        }
    }

    public struct SkipStateMachine<TContinuation, TSource> : IObserverStateMachine<TSource>
        where TContinuation : IObserverStateMachine<TSource>
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