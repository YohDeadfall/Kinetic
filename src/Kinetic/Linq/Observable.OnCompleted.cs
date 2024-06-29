using System;
using Kinetic.Linq.StateMachines;

namespace Kinetic.Linq;

public static partial class Observable
{
    public static ObserverBuilder<TSource> OnCompleted<TSource>(this IObservable<TSource> source, Action onCompleted) =>
        source.ToBuilder().OnCompleted(onCompleted);

    public static ObserverBuilder<TSource> OnCompleted<TSource>(this ObserverBuilder<TSource> source, Action onCompleted) =>
        source.ContinueWith<OnCompletedStateMachineFactory<TSource>, TSource>(new(onCompleted));

    private readonly struct OnCompletedStateMachineFactory<TSource> : IStateMachineFactory<TSource, TSource>
    {
        private readonly Action _onCompleted;

        public OnCompletedStateMachineFactory(Action onCompleted) =>
            _onCompleted = onCompleted;

        public void Create<TContinuation>(in TContinuation continuation, ObserverStateMachine<TSource> source)
            where TContinuation : struct, IStateMachine<TSource>
        {
            source.ContinueWith(new OnCompletedStateMachine<TSource, TContinuation>(continuation, _onCompleted));
        }
    }

    private struct OnCompletedStateMachine<TSource, TContinuation> : IStateMachine<TSource>
        where TContinuation : struct, IStateMachine<TSource>
    {
        private TContinuation _continuation;
        private readonly Action _onCompleted;

        public OnCompletedStateMachine(in TContinuation continuation, Action onCompleted)
        {
            _continuation = continuation;
            _onCompleted = onCompleted;
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

        public void OnNext(TSource value) =>
            _continuation.OnNext(value);

        public void OnError(Exception error) =>
            _continuation.OnError(error);

        public void OnCompleted()
        {
            try
            {
                _onCompleted();
            }
            catch (Exception error)
            {
                _continuation.OnError(error);
                return;
            }

            _continuation.OnCompleted();
        }
    }
}