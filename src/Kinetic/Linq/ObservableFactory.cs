using System;
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
        where TStateMachine : struct, IEntryStateMachine<TSource>
    {
        return new Box<TSource, TStateMachine>(stateMachine);
    }

    private interface IBox : IObservableInternal<TResult>
    {
        void Initialize(ref StateMachine publisher);
    }

    private sealed class Box<T, TStateMachine> : StateMachineBox<T, TStateMachine>, IBox
        where TStateMachine : struct, IEntryStateMachine<T>
    {
        private IntPtr _publisher;
        private bool _cold = true;

        public Box(TStateMachine stateMachine) :
            base(stateMachine) => StateMachine.Initialize(this);

        public void Initialize(ref StateMachine publisher) =>
            _publisher = OffsetTo<TResult, StateMachine>(ref publisher);

        public IDisposable Subscribe(IObserver<TResult> observer)
        {
            var subscription = GetSubscriptions().Subscribe(observer, this);
            Initialize();
            return subscription;
        }

        public void Subscribe(ObservableSubscription<TResult> subscription)
        {
            GetSubscriptions().Subscribe(subscription, this);
            Initialize();
        }

        public void Unsubscribe(ObservableSubscription<TResult> subscription) =>
            GetSubscriptions().Unsubscribe(subscription);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Initialize()
        {
            if (_cold)
                InitializeCore();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void InitializeCore() =>
            StateMachine.Start();

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

        public void OnCompleted()
        {
            _subscriptions.OnCompleted();
            _box!.Dispose();
        }

        public void OnError(Exception error)
        {
            _subscriptions.OnError(error);
            _box!.Dispose();
        }

        public void OnNext(TResult value) =>
            _subscriptions.OnNext(value);
    }
}