using System;

namespace Kinetic
{
    public static class Subscription
    {
        public static IDisposable Subscribe<T>(this IObservable<T> observable, Action<T> onNext) =>
            observable.ToBuilder().Subscribe(onNext);

        public static IDisposable Subscribe<T>(this IObservable<T> observable, Action<T> onNext, Action<Exception> onError) =>
            observable.ToBuilder().Subscribe(onNext, onError);

        public static IDisposable Subscribe<T>(this IObservable<T> observable, Action<T> onNext, Action onCompleted) =>
            observable.ToBuilder().Subscribe(onNext, onCompleted);

        public static IDisposable Subscribe<T>(this IObservable<T> observable, Action<T> onNext, Action<Exception> onError, Action onCompleted) =>
            observable.ToBuilder().Subscribe(onNext, onError, onCompleted);

        public static IDisposable Subscribe<T, TBuilder>(this TBuilder builder, Action<T> onNext)
            where TBuilder : struct, IObserverBuilder<T> =>
            builder.Subscribe(onNext, onError: static (error) => { }, onCompleted: static () => { });

        public static IDisposable Subscribe<T, TBuilder>(this TBuilder builder, Action<T> onNext, Action<Exception> onError)
            where TBuilder : struct, IObserverBuilder<T> =>
            builder.Subscribe(onNext, onError, onCompleted: static () => { });

        public static IDisposable Subscribe<T, TBuilder>(this TBuilder builder, Action<T> onNext, Action onCompleted)
            where TBuilder : struct, IObserverBuilder<T> =>
            builder.Subscribe(onNext, onError: static (error) => { }, onCompleted);

        public static IDisposable Subscribe<T, TBuilder>(
            this TBuilder builder,
            Action<T> onNext,
            Action<Exception> onError,
            Action onCompleted)
            where TBuilder : struct, IObserverBuilder<T>
        {
            var factory = new Factory();
            var stateMachine = new DelegatedStateMachine<T>(onNext, onError, onCompleted);

            builder.Build(stateMachine, ref factory);

            return factory.GetSubscription();
        }

        public static IDisposable Subscribe<T, TState>(this IObservable<T> observable, TState state, Action<TState, T> onNext) =>
            observable.ToBuilder().Subscribe(state, onNext);

        public static IDisposable Subscribe<T, TState>(this IObservable<T> observable, TState state, Action<TState, T> onNext, Action<TState, Exception> onError) =>
            observable.ToBuilder().Subscribe(state, onNext, onError);

        public static IDisposable Subscribe<T, TState>(this IObservable<T> observable, TState state, Action<TState, T> onNext, Action<TState> onCompleted) =>
            observable.ToBuilder().Subscribe(state, onNext, onCompleted);

        public static IDisposable Subscribe<T, TState>(this IObservable<T> observable, TState state, Action<TState, T> onNext, Action<TState, Exception> onError, Action<TState> onCompleted) =>
            observable.ToBuilder().Subscribe(state, onNext, onError, onCompleted);

        public static IDisposable Subscribe<T, TState, TBuilder>(this TBuilder builder, TState state, Action<TState, T> onNext)
            where TBuilder : struct, IObserverBuilder<T> =>
            builder.Subscribe(state, onNext, onError: static (state, error) => { }, onCompleted: static (state) => { });

        public static IDisposable Subscribe<T, TState, TBuilder>(this TBuilder builder, TState state, Action<TState, T> onNext, Action<TState, Exception> onError)
            where TBuilder : struct, IObserverBuilder<T> =>
            builder.Subscribe(state, onNext, onError, onCompleted: static (state) => { });

        public static IDisposable Subscribe<T, TState, TBuilder>(this TBuilder builder, TState state, Action<TState, T> onNext, Action<TState> onCompleted)
            where TBuilder : struct, IObserverBuilder<T> =>
            builder.Subscribe(state, onNext, onError: static (state, error) => { }, onCompleted);

        public static IDisposable Subscribe<T, TState, TBuilder>(
            this TBuilder builder,
            TState state,
            Action<TState, T> onNext,
            Action<TState, Exception> onError,
            Action<TState> onCompleted)
            where TBuilder : struct, IObserverBuilder<T>
        {
            var factory = new Factory();
            var stateMachine = new DelegatedStateMachine<T, TState>(state, onNext, onError, onCompleted);

            builder.Build(stateMachine, ref factory);

            return factory.GetSubscription();
        }

        private struct Factory : IObserverFactory
        {
            private IDisposable? _subscription;

            public void Create<TSource, TStateMachine>(in TStateMachine stateMachine)
                where TStateMachine : struct, IObserverStateMachine<TSource>
            {
                _subscription = new Observer<TSource, TStateMachine>(stateMachine);
            }

            public IDisposable GetSubscription()
            {
                return _subscription ?? throw new InvalidOperationException();
            }
        }

        private struct DelegatedStateMachine<T> : IObserverStateMachine<T>
        {
            private IDisposable? _box;
            private readonly Action<T> _onNext;
            private readonly Action<Exception> _onError;
            private readonly Action _onCompleted;

            public DelegatedStateMachine(
                Action<T> onNext,
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

            public void OnNext(T value)
            {
                _onNext(value);
            }

            public void OnError(Exception error)
            {
                _box!.Dispose();
                _onError(error);
            }

            public void OnCompleted()
            {
                _box!.Dispose();
                _onCompleted();
            }
        }

        private struct DelegatedStateMachine<T, TState> : IObserverStateMachine<T>
        {
            private IDisposable? _box;
            private readonly TState _state;
            private readonly Action<TState, T> _onNext;
            private readonly Action<TState, Exception> _onError;
            private readonly Action<TState> _onCompleted;

            public DelegatedStateMachine(
                TState state,
                Action<TState, T> onNext,
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

            public void OnNext(T value)
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

        private struct ObserverStateMachine<T> : IObserverStateMachine<T>
        {
            private IDisposable? _box;
            private readonly IObserver<T> _observer;

            public ObserverStateMachine(IObserver<T> observer)
            {
                _box = null;
                _observer = observer;
            }

            public void Initialize(IObserverStateMachineBox box) => _box = box;
            public void Dispose() { }

            public void OnNext(T value)
            {
                _observer.OnNext(value);
            }

            public void OnError(Exception error)
            {
                _box!.Dispose();
                _observer.OnError(error);
            }

            public void OnCompleted()
            {
                _box!.Dispose();
                _observer.OnCompleted();
            }
        }
    }
}