using System;
using System.Runtime.CompilerServices;
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
        ref ObservableSubscriptions<TResult> Subscriptions { get; }
    }

    private sealed class Box<T, TStateMachine> : StateMachineBox<T, TStateMachine>, IBox
        where TStateMachine : struct, IStateMachine<T>
    {
        private ObservableSubscriptions<TResult> _subscriptions;

        public ref ObservableSubscriptions<TResult> Subscriptions => ref _subscriptions;

        public Box(in TStateMachine stateMachine) :
            base(stateMachine) => StateMachine.Initialize(this);

        public IDisposable Subscribe(IObserver<TResult> observer) =>
            _subscriptions.Subscribe(this, observer);

        public void Subscribe(ObservableSubscription<TResult> subscription) =>
            _subscriptions.Subscribe(this, subscription);

        public void Unsubscribe(ObservableSubscription<TResult> subscription) =>
            _subscriptions.Unsubscribe(subscription);
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
        private IntPtr _subscriptions;

        public StateMachineBox Box =>
            _box ?? throw new InvalidOperationException();

        public StateMachine<TResult> Reference =>
            StateMachine<TResult>.Create(ref this);

        public StateMachine? Continuation =>
            null;

        public void Dispose() { }

        public void Initialize(StateMachineBox box)
        {
            var boxTyped = (IBox) box;

            _box = box;
            _subscriptions = Unsafe.ByteOffset(
                ref Unsafe.As<ObservableStateMachine, IntPtr>(ref this),
                ref Unsafe.As<ObservableSubscriptions<TResult>, IntPtr>(ref boxTyped.Subscriptions));
        }

        public void OnCompleted() =>
            GetSubscriptions(ref this).OnCompleted();

        public void OnError(Exception error) =>
            GetSubscriptions(ref this).OnError(error);

        public void OnNext(TResult value) =>
            GetSubscriptions(ref this).OnNext(value);

        private static ref ObservableSubscriptions<TResult> GetSubscriptions(ref ObservableStateMachine self) =>
            ref Unsafe.As<IntPtr, ObservableSubscriptions<TResult>>(
                ref Unsafe.AddByteOffset(
                    ref Unsafe.As<ObservableStateMachine, IntPtr>(ref self),
                    self._subscriptions));
    }
}