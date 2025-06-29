using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Kinetic.Runtime;

namespace Kinetic.Linq;

internal readonly struct ObservableFactory<TResult> : IStateMachineBoxFactory<IObservable<TResult>>
{
    public static IObservable<TResult> Create<TOperator>(TOperator source)
        where TOperator : IOperator<TResult>
    {
        return source.Build<IObservable<TResult>, ObservableFactory<TResult>, StateMachine>(
            new ObservableFactory<TResult>(), new StateMachine());
    }

    public IObservable<TResult> Create<TSource, TStateMachine>(TStateMachine stateMachine)
        where TStateMachine : struct, IStateMachine<TSource>
    {
        return stateMachine is ISubscribeStateMachine<TSource>
            ? new Cold<TSource, TStateMachine>(stateMachine)
            : new Hot<TSource, TStateMachine>(stateMachine);
    }

    private interface IBox : IObservableInternal<TResult>
    {
        void Initialize(ref StateMachine publisher);
    }

    private abstract class Box<T, TStateMachine> : StateMachineBox<T, TStateMachine>, IBox
        where TStateMachine : struct, IStateMachine<T>
    {
        private IntPtr _publisher;

        protected  Box(TStateMachine stateMachine) :
            base(stateMachine) => StateMachine.Initialize(this);

        protected ref ObservableSubscriptions<TResult> GetSubscriptions() =>
            ref ReferenceTo<TResult, StateMachine>(_publisher)._subscriptions;

        public void Initialize(ref StateMachine publisher) =>
            _publisher = OffsetTo<TResult, StateMachine>(ref publisher);

        public abstract IDisposable Subscribe(IObserver<TResult> observer);

        public abstract void Subscribe(ObservableSubscription<TResult> subscription);

        public abstract void Unsubscribe(ObservableSubscription<TResult> subscription);
    }

    private sealed class Cold<T, TStateMachine> : Box<T, TStateMachine>
        where TStateMachine : struct, IStateMachine<T>
    {
        private bool _cold = true;

        public Cold(TStateMachine stateMachine) :
            base(stateMachine) => throw new Exception("cold");

        public override IDisposable Subscribe(IObserver<TResult> observer)
        {
            Initialize();
            return GetSubscriptions().Subscribe(observer, this);
        }

        public override void Subscribe(ObservableSubscription<TResult> subscription)
        {
            Initialize();
            GetSubscriptions().Subscribe(subscription, this);
        }

        public override void Unsubscribe(ObservableSubscription<TResult> subscription) =>
            GetSubscriptions().Unsubscribe(subscription);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Initialize()
        {
            if (_cold)
                InitializeCore();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void InitializeCore()
        {
            Debug.Assert(StateMachine is ISubscribeStateMachine<T>);
            StateMachine.Initialize(this);

            _cold = false;
        }
    }

    private sealed class Hot<T, TStateMachine> : Box<T, TStateMachine>
        where TStateMachine : struct, IStateMachine<T>
    {
        public Hot(TStateMachine stateMachine) :
            base(stateMachine) { }

        public override IDisposable Subscribe(IObserver<TResult> observer) =>
            GetSubscriptions().Subscribe(observer, this);

        public override void Subscribe(ObservableSubscription<TResult> subscription) =>
            GetSubscriptions().Subscribe(subscription, this);

        public override void Unsubscribe(ObservableSubscription<TResult> subscription) =>
            GetSubscriptions().Unsubscribe(subscription);
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