using System;
using Kinetic.Linq.StateMachines;

namespace Kinetic.Linq;

public static partial class Observable
{
    public static IObservable<TSource> ToObservable<TSource>(this ObserverBuilder<TSource> source) =>
        source.Build<Observable<TSource>.ObservableStateMachine, Observable<TSource>.BoxFactory, IObservable<TSource>>(
            continuation: new(),
            factory: new());
}

internal static class Observable<TResult>
{
    private interface IBox : IObservableInternal<TResult>
    {
        void Initialize(ref ObservableStateMachine publisher);
    }

    private sealed class Box<T, TStateMachine> : StateMachineBox<T, TStateMachine>, IBox
        where TStateMachine : struct, IStateMachine<T>
    {
        private IntPtr _publisher;

        public Box(in TStateMachine stateMachine) :
            base(stateMachine) => StateMachine.Initialize(this);

        public IDisposable Subscribe(IObserver<TResult> observer) =>
            GetSubscriptions().Subscribe(this, observer);

        public void Subscribe(ObservableSubscription<TResult> subscription) =>
            GetSubscriptions().Subscribe(this, subscription);

        public void Unsubscribe(ObservableSubscription<TResult> subscription) =>
            GetSubscriptions().Unsubscribe(subscription);

        public void Initialize(ref Observable<TResult>.ObservableStateMachine publisher) =>
            _publisher = OffsetTo<TResult, ObservableStateMachine>(ref publisher);

        private ref ObservableSubscriptions<TResult> GetSubscriptions() =>
            ref ReferenceTo<TResult, ObservableStateMachine>(_publisher)._subscriptions;
    }

    internal readonly struct BoxFactory : IStateMachineBoxFactory<IObservable<TResult>>
    {
        public IObservable<TResult> Create<T, TStateMachine>(in TStateMachine stateMachine)
            where TStateMachine : struct, IStateMachine<T> =>
            new Box<T, TStateMachine>(stateMachine);
    }

    internal struct ObservableStateMachine : IStateMachine<TResult>
    {
        private StateMachineBox _box;
        internal ObservableSubscriptions<TResult> _subscriptions;

        public StateMachineBox Box =>
            _box ?? throw new InvalidOperationException();

        public StateMachine<TResult> Reference =>
            StateMachine<TResult>.Create(ref this);

        public StateMachine? Continuation =>
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