using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;

namespace Kinetic
{
    public static partial class Observable
    {
        private static readonly Action<Exception> OnError = (error) => ExceptionDispatchInfo.Throw(error);
        private static readonly Action OnCompleted = () => { };

        public static IDisposable Subscribe<TSource>(this IObservable<TSource> observable, Action<TSource> onNext) =>
            observable.ToBuilder().Subscribe(onNext);

        public static IDisposable Subscribe<TSource>(this IObservable<TSource> observable, Action<TSource> onNext, Action<Exception> onError) =>
            observable.ToBuilder().Subscribe(onNext, onError);

        public static IDisposable Subscribe<TSource>(this IObservable<TSource> observable, Action<TSource> onNext, Action onCompleted) =>
            observable.ToBuilder().Subscribe(onNext, onCompleted);

        public static IDisposable Subscribe<TSource>(this IObservable<TSource> observable, Action<TSource> onNext, Action<Exception> onError, Action onCompleted) =>
            observable.ToBuilder().Subscribe(onNext, onError, onCompleted);

        public static IDisposable Subscribe<TSource>(this in ObserverBuilder<TSource> observable, Action<TSource> onNext) =>
            observable.Subscribe(onNext, OnError, OnCompleted);

        public static IDisposable Subscribe<TSource>(this in ObserverBuilder<TSource> observable, Action<TSource> onNext, Action<Exception> onError) =>
            observable.Subscribe(onNext, onError, OnCompleted);

        public static IDisposable Subscribe<TSource>(this in ObserverBuilder<TSource> observable, Action<TSource> onNext, Action onCompleted) =>
            observable.Subscribe(onNext, OnError, onCompleted);

        public static IDisposable Subscribe<TSource>(this in ObserverBuilder<TSource> observable, Action<TSource> onNext, Action<Exception> onError, Action onCompleted)
        {
            var factory = new ObserverFactory();
            var stateMachine = new SubscribeStateMachine<TSource>(onNext, onError, onCompleted);

            return observable.Build<SubscribeStateMachine<TSource>, ObserverFactory, IDisposable>(stateMachine, factory);
        }

        public static IDisposable Subscribe<TSource, TState>(this IObservable<TSource> observable, TState state, Action<TState, TSource> onNext) =>
            observable.ToBuilder().Subscribe(state, onNext);

        public static IDisposable Subscribe<TSource, TState>(this IObservable<TSource> observable, TState state, Action<TState, TSource> onNext, Action<TState, Exception> onError) =>
            observable.ToBuilder().Subscribe(state, onNext, onError);

        public static IDisposable Subscribe<TSource, TState>(this IObservable<TSource> observable, TState state, Action<TState, TSource> onNext, Action<TState> onCompleted) =>
            observable.ToBuilder().Subscribe(state, onNext, onCompleted);

        public static IDisposable Subscribe<TSource, TState>(this IObservable<TSource> observable, TState state, Action<TState, TSource> onNext, Action<TState, Exception> onError, Action<TState> onCompleted) =>
            observable.ToBuilder().Subscribe(state, onNext, onError, onCompleted);

        public static IDisposable Subscribe<TSource, TState>(this in ObserverBuilder<TSource> observable, TState state, Action<TState, TSource> onNext) =>
            observable.Subscribe(state, onNext);

        public static IDisposable Subscribe<TSource, TState>(this in ObserverBuilder<TSource> observable, TState state, Action<TState, TSource> onNext, Action<TState, Exception> onError) =>
            observable.Subscribe(state, onNext, onError);

        public static IDisposable Subscribe<TSource, TState>(this in ObserverBuilder<TSource> observable, TState state, Action<TState, TSource> onNext, Action<TState> onCompleted) =>
            observable.Subscribe(state, onNext, onCompleted);

        public static IDisposable Subscribe<TSource, TState>(this in ObserverBuilder<TSource> observable, TState state, Action<TState, TSource> onNext, Action<TState, Exception> onError, Action<TState> onCompleted)
        {
            var factory = new ObserverFactory();
            var stateMachine = new SubscribeStateMachine<TSource, TState>(state, onNext, onError, onCompleted);

            return observable.Build<SubscribeStateMachine<TSource, TState>, ObserverFactory, IDisposable>(stateMachine, factory);
        }
    }

    internal struct SubscribeStateMachine<TSource> : IObserverStateMachine<TSource>
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

    internal struct SubscribeStateMachine<TSource, TState> : IObserverStateMachine<TSource>
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