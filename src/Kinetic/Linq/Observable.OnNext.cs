using System;
using Kinetic.Linq.StateMachines;

namespace Kinetic.Linq;

public static partial class Observable
{
    public static ObserverBuilder<T> OnNext<T>(this IObservable<T> source, Action<T> onNext) =>
        source.ToBuilder().OnNext(onNext);

    public static ObserverBuilder<T> OnNext<T>(this ObserverBuilder<T> source, Action<T> onNext) =>
        source.ContinueWith<OnNextStateMachineFactory<T>, T>(new(onNext));

    private readonly struct OnNextStateMachineFactory<T> : IStateMachineFactory<T, T>
    {
        private readonly Action<T> _onNext;

        public OnNextStateMachineFactory(Action<T> onNext) =>
            _onNext = onNext;

        public void Create<TContinuation>(in TContinuation continuation, ObserverStateMachine<T> source)
            where TContinuation : struct, IStateMachine<T>
        {
            source.ContinueWith(new OnNextStateMachine<TContinuation, T>(continuation, _onNext));
        }
    }

    private struct OnNextStateMachine<TContinuation, T> : IStateMachine<T>
        where TContinuation : struct, IStateMachine<T>
    {
        private TContinuation _continuation;
        private readonly Action<T> _onNext;

        public OnNextStateMachine(in TContinuation continuation, Action<T> onNext)
        {
            _continuation = continuation;
            _onNext = onNext;
        }

        public StateMachineBox Box =>
            _continuation.Box;

        public void Initialize(StateMachineBox box) =>
            _continuation.Initialize(box);

        public void Dispose() =>
            _continuation.Dispose();

        public void OnNext(T value)
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