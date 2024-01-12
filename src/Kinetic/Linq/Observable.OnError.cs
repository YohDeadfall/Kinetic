using System;
using Kinetic.Linq.StateMachines;

namespace Kinetic.Linq;

public static partial class Observable
{
    public static ObserverBuilder<T> OnError<T>(this IObservable<T> source, Action<Exception> onError) =>
        source.ToBuilder().OnError(onError);

    public static ObserverBuilder<T> OnError<T>(this ObserverBuilder<T> source, Action<Exception> onError) =>
        source.ContinueWith<OnErrorStateMachineFactory<T>, T>(new(onError));

    private readonly struct OnErrorStateMachineFactory<T> : IObserverStateMachineFactory<T, T>
    {
        private readonly Action<Exception> _onError;

        public OnErrorStateMachineFactory(Action<Exception> onError) =>
            _onError = onError;

        public void Create<TContinuation>(in TContinuation continuation, ObserverStateMachine<T> source)
            where TContinuation : struct, IObserverStateMachine<T>
        {
            source.ContinueWith(new OnErrorStateMachine<TContinuation, T>(continuation, _onError));
        }
    }

    private struct OnErrorStateMachine<TContinuation, T> : IObserverStateMachine<T>
        where TContinuation : struct, IObserverStateMachine<T>
    {
        private TContinuation _continuation;
        private readonly Action<Exception> _onError;

        public OnErrorStateMachine(in TContinuation continuation, Action<Exception> onError)
        {
            _continuation = continuation;
            _onError = onError;
        }

        public ObserverStateMachineBox Box =>
            _continuation.Box;

        public void Initialize(ObserverStateMachineBox box) =>
            _continuation.Initialize(box);

        public void Dispose() =>
            _continuation.Dispose();

        public void OnNext(T value) =>
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