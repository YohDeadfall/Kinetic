using System;

namespace Kinetic.Linq.StateMachines;

public static partial class Observable
{
    public static ObserverBuilder<T> Do<T, TStateMachine>(this IObservable<T> source, ref TStateMachine stateMachine)
        where TStateMachine : struct, IObserverStateMachine<T> =>
        source.ToBuilder().Do(ref stateMachine);

    public static ObserverBuilder<T> Do<T, TStateMachine>(this ObserverBuilder<T> source, ref TStateMachine stateMachine)
        where TStateMachine : struct, IObserverStateMachine<T> =>
        source.ContinueWith<DoStateMachineFactory<T, TStateMachine>, T>(new(new(ref stateMachine)));

    private readonly struct DoStateMachineFactory<T, TStateMachine> : IObserverStateMachineFactory<T, T>
        where TStateMachine : struct, IObserverStateMachine<T>
    {
        private readonly ObserverStateMachineReference<T, TStateMachine> _stateMachine;

        public DoStateMachineFactory(ObserverStateMachineReference<T, TStateMachine> stateMachine) =>
            _stateMachine = stateMachine;

        public void Create<TContinuation>(in TContinuation continuation, ObserverStateMachine<T> source)
            where TContinuation : struct, IObserverStateMachine<T>
        {
            source.ContinueWith<DoStateMachine<T, TStateMachine, TContinuation>>(new(continuation, _stateMachine));
        }
    }

    private struct DoStateMachine<T, TStateMachine, TContinuation> : IObserverStateMachine<T>
        where TStateMachine : struct, IObserverStateMachine<T>
        where TContinuation : struct, IObserverStateMachine<T>
    {
        private TContinuation _continuation;
        private readonly ObserverStateMachineReference<T, TStateMachine> _observer;

        public DoStateMachine(in TContinuation continuation, ObserverStateMachineReference<T, TStateMachine> observer)
        {
            _continuation = continuation;
            _observer = observer; ;
        }

        public ObserverStateMachineBox Box =>
            _continuation.Box;

        public void Dispose() =>
            _continuation.Dispose();

        public void Initialize(ObserverStateMachineBox box) =>
            _continuation.Initialize(box);

        public void OnCompleted()
        {
            try
            {
                _observer.Target.OnCompleted();
            }
            catch (Exception ex)
            {
                _continuation.OnError(ex);

                return;
            }

            _continuation.OnCompleted();
        }

        public void OnError(Exception error)
        {
            try
            {
                _observer.Target.OnError(error);
            }
            catch (Exception ex)
            {
                _continuation.OnError(ex);

                return;
            }

            _continuation.OnError(error);
        }

        public void OnNext(T value)
        {
            try
            {
                _observer.Target.OnNext(value);
            }
            catch (Exception ex)
            {
                _continuation.OnError(ex);

                return;
            }

            _continuation.OnNext(value);
        }
    }
}