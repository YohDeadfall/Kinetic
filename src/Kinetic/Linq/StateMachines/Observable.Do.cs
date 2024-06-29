using System;

namespace Kinetic.Linq.StateMachines;

public static partial class Observable
{
    public static ObserverBuilder<T> Do<T, TStateMachine>(this IObservable<T> source, ref TStateMachine stateMachine)
        where TStateMachine : struct, IStateMachine<T> =>
        source.ToBuilder().Do(ref stateMachine);

    public static ObserverBuilder<T> Do<T, TStateMachine>(this ObserverBuilder<T> source, ref TStateMachine stateMachine)
        where TStateMachine : struct, IStateMachine<T> =>
        source.ContinueWith<DoStateMachineFactory<T, TStateMachine>, T>(new(new(ref stateMachine)));

    private readonly struct DoStateMachineFactory<T, TStateMachine> : IStateMachineFactory<T, T>
        where TStateMachine : struct, IStateMachine<T>
    {
        private readonly StateMachineReference<T, TStateMachine> _stateMachine;

        public DoStateMachineFactory(StateMachineReference<T, TStateMachine> stateMachine) =>
            _stateMachine = stateMachine;

        public void Create<TContinuation>(in TContinuation continuation, ObserverStateMachine<T> source)
            where TContinuation : struct, IStateMachine<T>
        {
            source.ContinueWith<DoStateMachine<T, TStateMachine, TContinuation>>(new(continuation, _stateMachine));
        }
    }

    private struct DoStateMachine<T, TStateMachine, TContinuation> : IStateMachine<T>
        where TStateMachine : struct, IStateMachine<T>
        where TContinuation : struct, IStateMachine<T>
    {
        private TContinuation _continuation;
        private readonly StateMachineReference<T, TStateMachine> _observer;

        public DoStateMachine(in TContinuation continuation, StateMachineReference<T, TStateMachine> observer)
        {
            _continuation = continuation;
            _observer = observer; ;
        }

        public StateMachineBox Box =>
            _continuation.Box;

        public StateMachine<T> Reference =>
            StateMachine<T>.Create(ref this);

        public StateMachine? Continuation =>
            _continuation.Reference;

        public void Initialize(StateMachineBox box) =>
            _continuation.Initialize(box);

        public void Dispose() =>
            _continuation.Dispose();

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