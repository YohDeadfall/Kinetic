using System;
using Kinetic.Linq.StateMachines;

namespace Kinetic.Linq;

public static partial class Observable
{
    public static ObserverBuilder<TSource> OnError<TSource>(this IObservable<TSource> source, Action<Exception> onError) =>
        source.ToBuilder().OnError(onError);

    public static ObserverBuilder<TSource> OnError<TSource>(this ObserverBuilder<TSource> source, Action<Exception> onError) =>
        source.ContinueWith<OnErrorStateMachineFactory<TSource>, TSource>(new(onError));

    private readonly struct OnErrorStateMachineFactory<TSource> : IStateMachineFactory<TSource, TSource>
    {
        private readonly Action<Exception> _onError;

        public OnErrorStateMachineFactory(Action<Exception> onError) =>
            _onError = onError;

        public void Create<TContinuation>(in TContinuation continuation, ObserverStateMachine<TSource> source)
            where TContinuation : struct, IStateMachine<TSource>
        {
            source.ContinueWith(new OnErrorStateMachine<TSource, TContinuation>(continuation, _onError));
        }
    }

    private struct OnErrorStateMachine<TSource, TContinuation> : IStateMachine<TSource>
        where TContinuation : struct, IStateMachine<TSource>
    {
        private TContinuation _continuation;
        private readonly Action<Exception> _onError;

        public OnErrorStateMachine(in TContinuation continuation, Action<Exception> onError)
        {
            _continuation = continuation;
            _onError = onError;
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

        public void OnError(Exception error)
        {
            try
            {
                _onError(error);
            }
            catch (Exception errorOnceAgain)
            {
                error = errorOnceAgain;
            }

            _continuation.OnError(error);
        }

        public void OnCompleted() =>
            _continuation.OnCompleted();
    }
}