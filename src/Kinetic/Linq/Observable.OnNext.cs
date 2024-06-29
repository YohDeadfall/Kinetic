using System;
using Kinetic.Linq.StateMachines;

namespace Kinetic.Linq;

public static partial class Observable
{
    public static ObserverBuilder<TSource> OnNext<TSource>(this IObservable<TSource> source, Action<TSource> onNext) =>
        source.ToBuilder().OnNext(onNext);

    public static ObserverBuilder<TSource> OnNext<TSource>(this ObserverBuilder<TSource> source, Action<TSource> onNext) =>
        source.ContinueWith<OnNextStateMachineFactory<TSource>, TSource>(new(onNext));

    private readonly struct OnNextStateMachineFactory<TSource> : IStateMachineFactory<TSource, TSource>
    {
        private readonly Action<TSource> _onNext;

        public OnNextStateMachineFactory(Action<TSource> onNext) =>
            _onNext = onNext;

        public void Create<TContinuation>(in TContinuation continuation, ObserverStateMachine<TSource> source)
            where TContinuation : struct, IStateMachine<TSource>
        {
            source.ContinueWith(new OnNextStateMachine<TSource, TContinuation>(continuation, _onNext));
        }
    }

    private struct OnNextStateMachine<TSource, TContinuation> : IStateMachine<TSource>
        where TContinuation : struct, IStateMachine<TSource>
    {
        private TContinuation _continuation;
        private readonly Action<TSource> _onNext;

        public OnNextStateMachine(in TContinuation continuation, Action<TSource> onNext)
        {
            _continuation = continuation;
            _onNext = onNext;
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
                _onNext(value);
            }
            catch (Exception error)
            {
                _continuation.OnError(error);
                return;
            }

            _continuation.OnNext(value);
        }

        public void OnError(Exception error) =>
            _continuation.OnError(error);

        public void OnCompleted() =>
            _continuation.OnCompleted();
    }
}