using System;
using Kinetic.Linq.StateMachines;

namespace Kinetic.Linq
{
    public static partial class Observable
    {
        public static ObserverBuilder<bool> All(this in ObserverBuilder<bool> source) =>
            source.ContinueWith<AllStateMachineFactory, bool>(default);

        public static ObserverBuilder<bool> All<TSource>(this in ObserverBuilder<TSource> source, Func<TSource, bool> predicate) =>
            source.Select(predicate).All();

        public static ObserverBuilder<bool> All(this IObservable<bool> source) =>
            source.ToBuilder().All();

        public static ObserverBuilder<bool> All<TSource>(this IObservable<TSource> source, Func<TSource, bool> predicate) =>
            source.ToBuilder().All(predicate);
    }

    internal readonly struct AllStateMachineFactory : IObserverStateMachineFactory<bool, bool>
    {
        public void Create<TContinuation>(in TContinuation continuation, ObserverStateMachine<bool> source)
            where TContinuation : struct, IObserverStateMachine<bool>
        {
            source.ContinueWith(new AllStateMachine<TContinuation>(continuation));
        }
    }

    internal struct AllStateMachine<TContinuation> : IObserverStateMachine<bool>
        where TContinuation : IObserverStateMachine<bool>
    {
        private TContinuation _continuation;

        public AllStateMachine(in TContinuation continuation) => _continuation = continuation;
        public void Initialize(IObserverStateMachineBox box) => _continuation.Initialize(box);
        public void Dispose() => _continuation.Dispose();

        public void OnNext(bool value)
        {
            if (!value)
            {
                _continuation.OnNext(false);
                _continuation.OnCompleted();
            }
        }

        public void OnError(Exception error)
        {
            _continuation.OnError(error);
        }

        public void OnCompleted()
        {
            _continuation.OnNext(true);
            _continuation.OnCompleted();
        }
    }
}