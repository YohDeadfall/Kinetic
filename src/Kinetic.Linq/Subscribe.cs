using System;
using System.Diagnostics.CodeAnalysis;
using Kinetic.Linq.StateMachines;

namespace Kinetic.Linq;

public static partial class Observable
{
    public static IDisposable Subscribe<TSource>(this IObservable<TSource> source, Action<TSource> onNext) =>
        source.ToBuilder().Subscribe(onNext);

    public static IDisposable Subscribe<TSource>(this IObservable<TSource> source, Action<TSource> onNext, Action<Exception> onError) =>
        source.ToBuilder().Subscribe(onNext, onError);

    public static IDisposable Subscribe<TSource>(this IObservable<TSource> source, Action<TSource> onNext, Action onCompleted) =>
        source.ToBuilder().Subscribe(onNext, onCompleted);

    public static IDisposable Subscribe<TSource>(this IObservable<TSource> source, Action<TSource> onNext, Action<Exception> onError, Action onCompleted) =>
        source.ToBuilder().Subscribe(onNext, onError, onCompleted);

    public static IDisposable Subscribe<TSource>(this in ObserverBuilder<TSource> source, Action<TSource> onNext) =>
        source.Subscribe(onNext, ThrowOnError, NothingOnCompleted);

    public static IDisposable Subscribe<TSource>(this in ObserverBuilder<TSource> source, Action<TSource> onNext, Action<Exception> onError) =>
        source.Subscribe(onNext, onError, NothingOnCompleted);

    public static IDisposable Subscribe<TSource>(this in ObserverBuilder<TSource> source, Action<TSource> onNext, Action onCompleted) =>
        source.Subscribe(onNext, ThrowOnError, onCompleted);

    public static IDisposable Subscribe<TSource>(this in ObserverBuilder<TSource> source, Action<TSource> onNext, Action<Exception> onError, Action onCompleted) =>
        source.Build<SubscribeStateMachine<TSource>, ObserverStateMachineBoxFactory, IDisposable>(
            continuation: new(onNext, onError, onCompleted),
            factory: new());

    public static IDisposable Subscribe<TSource, TState>(this IObservable<TSource> source, TState state, Action<TState, TSource> onNext) =>
        source.ToBuilder().Subscribe(state, onNext);

    public static IDisposable Subscribe<TSource, TState>(this IObservable<TSource> source, TState state, Action<TState, TSource> onNext, Action<TState, Exception> onError) =>
        source.ToBuilder().Subscribe(state, onNext, onError);

    public static IDisposable Subscribe<TSource, TState>(this IObservable<TSource> source, TState state, Action<TState, TSource> onNext, Action<TState> onCompleted) =>
        source.ToBuilder().Subscribe(state, onNext, onCompleted);

    public static IDisposable Subscribe<TSource, TState>(this IObservable<TSource> source, TState state, Action<TState, TSource> onNext, Action<TState, Exception> onError, Action<TState> onCompleted) =>
        source.ToBuilder().Subscribe(state, onNext, onError, onCompleted);

    public static IDisposable Subscribe<TSource, TState>(this in ObserverBuilder<TSource> source, TState state, Action<TState, TSource> onNext) =>
        source.Subscribe(state, onNext);

    public static IDisposable Subscribe<TSource, TState>(this in ObserverBuilder<TSource> source, TState state, Action<TState, TSource> onNext, Action<TState, Exception> onError) =>
        source.Subscribe(state, onNext, onError);

    public static IDisposable Subscribe<TSource, TState>(this in ObserverBuilder<TSource> source, TState state, Action<TState, TSource> onNext, Action<TState> onCompleted) =>
        source.Subscribe(state, onNext, onCompleted);

    public static IDisposable Subscribe<TSource, TState>(this in ObserverBuilder<TSource> source, TState state, Action<TState, TSource> onNext, Action<TState, Exception> onError, Action<TState> onCompleted) =>
        source.Build<SubscribeStateMachine<TSource, TState>, ObserverStateMachineBoxFactory, IDisposable>(
            continuation: new(state, onNext, onError, onCompleted),
            factory: new());

    private struct SubscribeStateMachine<TSource> : IObserverStateMachine<TSource>
    {
        [AllowNull]
        private IDisposable _box;
        private readonly Action<TSource> _onNext;
        private readonly Action<Exception> _onError;
        private readonly Action _onCompleted;

        public SubscribeStateMachine(
            Action<TSource> onNext,
            Action<Exception> onError,
            Action onCompleted)
        {
            _box = null;
            _onNext = onNext;
            _onError = onError;
            _onCompleted = onCompleted;
        }

        public void Initialize(IObserverStateMachineBox box) => _box = box;
        public void Dispose() { }

        public void OnNext(TSource value)
        {
            _onNext(value);
        }

        public void OnError(Exception error)
        {
            _box.Dispose();
            _onError(error);
        }

        public void OnCompleted()
        {
            _box.Dispose();
            _onCompleted();
        }
    }

    private struct SubscribeStateMachine<TSource, TState> : IObserverStateMachine<TSource>
    {
        private IDisposable? _box;
        private readonly TState _state;
        private readonly Action<TState, TSource> _onNext;
        private readonly Action<TState, Exception> _onError;
        private readonly Action<TState> _onCompleted;

        public SubscribeStateMachine(
            TState state,
            Action<TState, TSource> onNext,
            Action<TState, Exception> onError,
            Action<TState> onCompleted)
        {
            _box = null;
            _state = state;
            _onNext = onNext;
            _onError = onError;
            _onCompleted = onCompleted;
        }

        public void Initialize(IObserverStateMachineBox box) => _box = box;
        public void Dispose() { }

        public void OnNext(TSource value)
        {
            _onNext(_state, value);
        }

        public void OnError(Exception error)
        {
            _box!.Dispose();
            _onError(_state, error);
        }

        public void OnCompleted()
        {
            _box!.Dispose();
            _onCompleted(_state);
        }
    }
}