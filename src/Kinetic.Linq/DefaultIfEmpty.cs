using System;
using System.Collections.Generic;
using Kinetic.Linq.StateMachines;

namespace Kinetic.Linq
{
    public static partial class Observable
    {
        public static ObserverBuilder<TSource?> DefaultIfEmpty<TSource>(this in ObserverBuilder<TSource> source) =>
            source.DefaultIfEmpty(default);

        public static ObserverBuilder<TSource?> DefaultIfEmpty<TSource>(this in ObserverBuilder<TSource> source, TSource? defaultValue) =>
            source.ContinueWith<DefaultIfEmptyStateMachineFactory<TSource>, TSource?>(new(defaultValue));

        public static ObserverBuilder<TSource?> DefaultIfEmpty<TSource>(this IObservable<TSource> source) =>
            source.ToBuilder().DefaultIfEmpty();

        public static ObserverBuilder<TSource?> DefaultIfEmpty<TSource>(this IObservable<TSource> source, TSource? defaultValue) =>
            source.ToBuilder().DefaultIfEmpty(defaultValue);
    }

    internal readonly struct DefaultIfEmptyStateMachineFactory<TSource> : IObserverStateMachineFactory<TSource, TSource?>
    {
        private readonly TSource? _defaultValue;

        public DefaultIfEmptyStateMachineFactory(TSource? defaultValue) => _defaultValue = defaultValue;

        public void Create<TContinuation>(in TContinuation continuation, ObserverStateMachine<TSource> source)
            where TContinuation : struct, IObserverStateMachine<TSource?>
        {
            source.ContinueWith(new DefaultIfEmptyStateMachine<TContinuation, TSource>(continuation, _defaultValue));
        }
    }

    internal struct DefaultIfEmptyStateMachine<TContinuation, TSource> : IObserverStateMachine<TSource>
        where TContinuation : IObserverStateMachine<TSource?>
    {
        private TContinuation _continuation;
        private readonly TSource? _defaultValue;
        private bool _isEmpty;

        public DefaultIfEmptyStateMachine(TContinuation continuation, TSource? defaultValue)
        {
            _continuation = continuation;
            _defaultValue = defaultValue;
            _isEmpty = true;
        }

        public void Initialize(IObserverStateMachineBox box) => _continuation.Initialize(box);
        public void Dispose() => _continuation.Dispose();

        public void OnNext(TSource value)
        {
            _isEmpty = false;
            _continuation.OnNext(value);
        }

        public void OnError(Exception error)
        {
            _continuation.OnError(error);
        }

        public void OnCompleted()
        {
            if (_isEmpty)
            {
                _continuation.OnNext(_defaultValue);
            }

            _continuation.OnCompleted();
        }
    }
}