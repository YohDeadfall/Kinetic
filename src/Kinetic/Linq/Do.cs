using System;
using Kinetic.Linq.StateMachines;

namespace Kinetic.Linq;

public static partial class Observable
{
    public static ObserverBuilder<TSource> Do<TSource>(this IObservable<TSource> source, Action<TSource> onNext) =>
        source.ToBuilder().Do(onNext);

    public static ObserverBuilder<TSource> Do<TSource>(this IObservable<TSource> source, Action<TSource> onNext, Action<Exception> onError) =>
        source.ToBuilder().Do(onNext, onError);

    public static ObserverBuilder<TSource> Do<TSource>(this IObservable<TSource> source, Action<TSource> onNext, Action onCompleted) =>
        source.ToBuilder().Do(onNext, onCompleted);

    public static ObserverBuilder<TSource> Do<TSource>(this IObservable<TSource> source, Action<TSource> onNext, Action<Exception> onError, Action onCompleted) =>
        source.ToBuilder().Do(onNext, onError, onCompleted);

    public static ObserverBuilder<TSource> Do<TSource>(this in ObserverBuilder<TSource> source, Action<TSource> onNext) =>
        source.Do(onNext, NothingOnError, NothingOnCompleted);

    public static ObserverBuilder<TSource> Do<TSource>(this in ObserverBuilder<TSource> source, Action<TSource> onNext, Action<Exception> onError) =>
        source.Do(onNext, onError, NothingOnCompleted);

    public static ObserverBuilder<TSource> Do<TSource>(this in ObserverBuilder<TSource> source, Action<TSource> onNext, Action onCompleted) =>
        source.Do(onNext, NothingOnError, onCompleted);

    public static ObserverBuilder<TSource> Do<TSource>(this in ObserverBuilder<TSource> source, Action<TSource> onNext, Action<Exception> onError, Action onCompleted) =>
        source.ContinueWith<DoStateMachineFactory<TSource>, TSource>(new(onNext, onError, onCompleted));

    public static ObserverBuilder<TSource> Do<TSource, TState>(this IObservable<TSource> source, TState state, Action<TState, TSource> onNext) =>
        source.ToBuilder().Do(state, onNext);

    public static ObserverBuilder<TSource> Do<TSource, TState>(this IObservable<TSource> source, TState state, Action<TState, TSource> onNext, Action<TState, Exception> onError) =>
        source.ToBuilder().Do(state, onNext, onError);

    public static ObserverBuilder<TSource> Do<TSource, TState>(this IObservable<TSource> source, TState state, Action<TState, TSource> onNext, Action<TState> onCompleted) =>
        source.ToBuilder().Do(state, onNext, onCompleted);

    public static ObserverBuilder<TSource> Do<TSource, TState>(this IObservable<TSource> source, TState state, Action<TState, TSource> onNext, Action<TState, Exception> onError, Action<TState> onCompleted) =>
        source.ToBuilder().Do(state, onNext, onError, onCompleted);

    public static ObserverBuilder<TSource> Do<TSource, TState>(this in ObserverBuilder<TSource> source, TState state, Action<TState, TSource> onNext) =>
        source.Do(state, onNext);

    public static ObserverBuilder<TSource> Do<TSource, TState>(this in ObserverBuilder<TSource> source, TState state, Action<TState, TSource> onNext, Action<TState, Exception> onError) =>
        source.Do(state, onNext, onError);

    public static ObserverBuilder<TSource> Do<TSource, TState>(this in ObserverBuilder<TSource> source, TState state, Action<TState, TSource> onNext, Action<TState> onCompleted) =>
        source.Do(state, onNext, onCompleted);

    public static ObserverBuilder<TSource> Do<TSource, TState>(this in ObserverBuilder<TSource> source, TState state, Action<TState, TSource> onNext, Action<TState, Exception> onError, Action<TState> onCompleted) =>
        source.ContinueWith<DoStateMachineFactory<TSource, TState>, TSource>(new(state, onNext, onError, onCompleted));

    private struct DoStateMachineFactory<TSource> : IObserverStateMachineFactory<TSource, TSource>
    {
        private readonly Action<TSource> _onNext;
        private readonly Action<Exception> _onError;
        private readonly Action _onCompleted;

        public DoStateMachineFactory(
            Action<TSource> onNext,
            Action<Exception> onError,
            Action onCompleted)
        {
            _onNext = onNext;
            _onError = onError;
            _onCompleted = onCompleted;
        }

        public void Create<TContinuation>(in TContinuation continuation, ObserverStateMachine<TSource> source)
            where TContinuation : struct, IObserverStateMachine<TSource>
        {
            source.ContinueWith(new DoStateMachine<TContinuation, TSource>(continuation, _onNext, _onError, _onCompleted));
        }
    }

    private struct DoStateMachine<TContinuation, TSource> : IObserverStateMachine<TSource>
        where TContinuation : struct, IObserverStateMachine<TSource>
    {
        private TContinuation _continuation;
        private readonly Action<TSource> _onNext;
        private readonly Action<Exception> _onError;
        private readonly Action _onCompleted;

        public DoStateMachine(
            in TContinuation continuation,
            Action<TSource> onNext,
            Action<Exception> onError,
            Action onCompleted)
        {
            _continuation = continuation;
            _onNext = onNext;
            _onError = onError;
            _onCompleted = onCompleted;
        }

        public void Initialize(IObserverStateMachineBox box) => _continuation.Initialize(box);
        public void Dispose() => _continuation.Dispose();

        public void OnNext(TSource value)
        {
            _onNext(value);
            _continuation.OnNext(value);
        }

        public void OnError(Exception error)
        {
            _onError(error);
            _continuation.OnError(error);
        }

        public void OnCompleted()
        {
            _onCompleted();
            _continuation.OnCompleted();
        }
    }

    private struct DoStateMachineFactory<TSource, TState> : IObserverStateMachineFactory<TSource, TSource>
    {
        private readonly TState _state;
        private readonly Action<TState, TSource> _onNext;
        private readonly Action<TState, Exception> _onError;
        private readonly Action<TState> _onCompleted;

        public DoStateMachineFactory(
            TState state,
            Action<TState, TSource> onNext,
            Action<TState, Exception> onError,
            Action<TState> onCompleted)
        {
            _state = state;
            _onNext = onNext;
            _onError = onError;
            _onCompleted = onCompleted;
        }

        public void Create<TContinuation>(in TContinuation continuation, ObserverStateMachine<TSource> source)
            where TContinuation : struct, IObserverStateMachine<TSource>
        {
            source.ContinueWith(new DoStateMachine<TContinuation, TSource, TState>(continuation, _state, _onNext, _onError, _onCompleted));
        }
    }

    private struct DoStateMachine<TContinuation, TSource, TState> : IObserverStateMachine<TSource>
        where TContinuation : struct, IObserverStateMachine<TSource>
    {
        private TContinuation _continuation;
        private readonly TState _state;
        private readonly Action<TState, TSource> _onNext;
        private readonly Action<TState, Exception> _onError;
        private readonly Action<TState> _onCompleted;

        public DoStateMachine(
            in TContinuation continuation,
            TState state,
            Action<TState, TSource> onNext,
            Action<TState, Exception> onError,
            Action<TState> onCompleted)
        {
            _continuation = continuation;
            _state = state;
            _onNext = onNext;
            _onError = onError;
            _onCompleted = onCompleted;
        }

        public void Initialize(IObserverStateMachineBox box) => _continuation.Initialize(box);
        public void Dispose() => _continuation.Dispose();

        public void OnNext(TSource value)
        {
            _onNext(_state, value);
            _continuation.OnNext(value);
        }

        public void OnError(Exception error)
        {
            _onError(_state, error);
            _continuation.OnError(error);
        }

        public void OnCompleted()
        {
            _onCompleted(_state);
            _continuation.OnCompleted();
        }
    }
}