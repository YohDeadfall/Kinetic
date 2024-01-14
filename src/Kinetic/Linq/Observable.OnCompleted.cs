using System;
using Kinetic.Linq.StateMachines;

namespace Kinetic.Linq;

public static partial class Observable
{
    public static ObserverBuilder<T> OnCompleted<T>(this IObservable<T> source, Action onCompleted) =>
        source.ToBuilder().OnCompleted(onCompleted);

    public static ObserverBuilder<T> OnCompleted<T>(this ObserverBuilder<T> source, Action onCompleted) =>
        source.ContinueWith<OnCompletedStateMachineFactory<T>, T>(new(onCompleted));

    private readonly struct OnCompletedStateMachineFactory<T> : IStateMachineFactory<T, T>
    {
        private readonly Action _onCompleted;

        public OnCompletedStateMachineFactory(Action onCompleted) =>
            _onCompleted = onCompleted;

        public void Create<TContinuation>(in TContinuation continuation, ObserverStateMachine<T> source)
            where TContinuation : struct, IStateMachine<T>
        {
            source.ContinueWith(new OnCompletedStateMachine<TContinuation, T>(continuation, _onCompleted));
        }
    }

    private struct OnCompletedStateMachine<TContinuation, T> : IStateMachine<T>
        where TContinuation : struct, IStateMachine<T>
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

        public void Initialize(StateMachineBox box) =>
            _continuation.Initialize(box);

        public void Dispose() =>
            _continuation.Dispose();

        public void OnNext(T value) =>
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