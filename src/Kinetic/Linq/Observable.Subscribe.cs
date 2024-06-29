using System;
using System.Runtime.ExceptionServices;
using Kinetic.Linq.StateMachines;

namespace Kinetic.Linq;

public static partial class Observable
{
    public static IDisposable Subscribe<TSource>(this IObservable<TSource> source) =>
        source.ToBuilder().Subscribe();

    public static IDisposable Subscribe<TSource>(this IObservable<TSource> source, Action<TSource> onNext) =>
        source.ToBuilder().Subscribe(onNext);

    public static IDisposable Subscribe<TSource>(this IObservable<TSource> source, Action<TSource> onNext, Action<Exception> onError) =>
        source.ToBuilder().Subscribe(onNext, onError);

    public static IDisposable Subscribe<TSource>(this IObservable<TSource> source, Action<TSource> onNext, Action onCompleted) =>
        source.ToBuilder().Subscribe(onNext, onCompleted);

    public static IDisposable Subscribe<TSource>(this IObservable<TSource> source, Action<TSource> onNext, Action<Exception> onError, Action onCompleted) =>
        source.ToBuilder().Subscribe(onNext, onError, onCompleted);

    public static IDisposable Subscribe<TSource>(this ObserverBuilder<TSource> source) =>
        source.Build<SubscribeStateMachine<TSource>, SubscribeBoxFactory, IDisposable>(
            continuation: new(),
            factory: new());

    public static IDisposable Subscribe<TSource>(this ObserverBuilder<TSource> source, Action<TSource> onNext) =>
        source.Do(onNext).Subscribe();

    public static IDisposable Subscribe<TSource>(this ObserverBuilder<TSource> source, Action<TSource> onNext, Action<Exception> onError) =>
        source.Do(onNext, onError).Subscribe();

    public static IDisposable Subscribe<TSource>(this ObserverBuilder<TSource> source, Action<TSource> onNext, Action onCompleted) =>
        source.Do(onNext, onCompleted).Subscribe();

    public static IDisposable Subscribe<TSource>(this ObserverBuilder<TSource> source, Action<TSource> onNext, Action<Exception> onError, Action onCompleted) =>
        source.Do(onNext, onError, onCompleted).Subscribe();

    private sealed class SubscribeBox<TSource, TStateMachine> : StateMachineBox<TSource, TStateMachine>, IDisposable
        where TStateMachine : struct, IStateMachine<TSource>
    {
        public SubscribeBox(in TStateMachine stateMachine) :
            base(stateMachine) => StateMachine.Initialize(this);

        public void Dispose() =>
            StateMachine.Dispose();
    }

    private readonly struct SubscribeBoxFactory : IStateMachineBoxFactory<IDisposable>
    {
        public IDisposable Create<T, TStateMachine>(in TStateMachine stateMachine)
            where TStateMachine : struct, IStateMachine<T> =>
            new SubscribeBox<T, TStateMachine>(stateMachine);
    }

    private struct SubscribeStateMachine<TSource> : IStateMachine<TSource>
    {
        private StateMachineBox? _box;

        public StateMachineBox Box =>
            _box ?? throw new InvalidOperationException();

        public StateMachine<TSource> Reference =>
            StateMachine<TSource>.Create(ref this);

        public StateMachine? Continuation =>
            null;

        public void Initialize(StateMachineBox box) =>
            _box = box;

        public void Dispose() =>
            _box = null;

        public void OnCompleted() { }

        public void OnError(Exception error) =>
            ExceptionDispatchInfo.Capture(error).Throw();

        public void OnNext(TSource value) { }
    }
}