using System;
using Kinetic.Runtime;

namespace Kinetic.Linq;

internal readonly struct ObservableFactory<TResult> : IStateMachineBoxFactory<IObservable<TResult>>
{
    public static IObservable<TResult> Create<TOperator>(in Operator<TOperator, TResult> source)
        where TOperator : IOperator<TResult>
    {
        return source.Build<IObservable<TResult>, ObservableFactory<TResult>, StateMachine>(
            new ObservableFactory<TResult>(), new StateMachine());
    }

    public IObservable<TResult> Create<TSource, TStateMachine>(TStateMachine stateMachine)
        where TStateMachine : struct, IStateMachine<TSource>
    {
        return new Box<TSource, TStateMachine>(stateMachine);
    }

    private interface IBox : IObservableInternal<TResult>
    {
        void Initialize(ref StateMachine publisher);
    }

    private sealed class Box<T, TStateMachine> : StateMachineBox<T, TStateMachine>, IBox
        where TStateMachine : struct, IStateMachine<T>
    {
        private IntPtr _publisher;

        public Box(in TStateMachine stateMachine) :
            base(stateMachine) => StateMachine.Initialize(this);

        public void Initialize(ref StateMachine publisher) =>
            _publisher = OffsetTo<TResult, StateMachine>(ref publisher);

        public IDisposable Subscribe(IObserver<TResult> observer) =>
            GetSubscriptions().Subscribe(observer, this);

        public void Subscribe(ObservableSubscription<TResult> subscription) =>
            GetSubscriptions().Subscribe(subscription, this);

        public void Unsubscribe(ObservableSubscription<TResult> subscription) =>
            GetSubscriptions().Unsubscribe(subscription);

        private ref ObservableSubscriptions<TResult> GetSubscriptions() =>
            ref ReferenceTo<TResult, StateMachine>(_publisher)._subscriptions;
    }

    internal struct StateMachine : IStateMachine<TResult>
    {
        private StateMachineBox? _box;
        internal ObservableSubscriptions<TResult> _subscriptions;

        public StateMachineBox Box =>
            _box ?? throw new InvalidOperationException();

        public StateMachineReference<TResult> Reference =>
            StateMachineReference<TResult>.Create(ref this);

        public StateMachineReference? Continuation =>
            null;

        public void Dispose() { }

        public void Initialize(StateMachineBox box)
        {
            ((IBox) box).Initialize(ref this);

            _box = box;
        }

        public void OnCompleted() =>
            _subscriptions.OnCompleted();

        public void OnError(Exception error) =>
            _subscriptions.OnError(error);

        public void OnNext(TResult value) =>
            _subscriptions.OnNext(value);
    }
}