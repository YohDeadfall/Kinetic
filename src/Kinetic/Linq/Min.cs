using System;
using System.Collections.Generic;
using Kinetic.Linq.StateMachines;

namespace Kinetic.Linq;

public static partial class Observable
{
    public static ObserverBuilder<TSource> Min<TSource>(this in ObserverBuilder<TSource> source, IComparer<TSource>? comparer = null) =>
        source.ContinueWith<MinStateMachineBuilder<TSource>, TSource>(new(comparer));

    public static ObserverBuilder<TSource> Min<TSource>(this IObservable<TSource> source, IComparer<TSource>? comparer = null) =>
        source.ToBuilder().Min(comparer);

    private readonly struct MinStateMachineBuilder<TSource> : IObserverStateMachineFactory<TSource, TSource>
    {
        private readonly IComparer<TSource>? _comparer;

        public MinStateMachineBuilder(IComparer<TSource>? comparer) => _comparer = comparer;

        public void Create<TContinuation>(in TContinuation continuation, ObserverStateMachine<TSource> source)
            where TContinuation : struct, IObserverStateMachine<TSource>
        {
            source.ContinueWith(new MinStateMachine<TContinuation, TSource>(continuation, _comparer));
        }
    }

    private struct MinStateMachine<TContinuation, T> : IObserverStateMachine<T>
        where TContinuation : struct, IObserverStateMachine<T>
    {
        private TContinuation _continuation;
        private IComparer<T>? _comparer;
        private T _value;
        private bool _hasValue;

        public MinStateMachine(TContinuation continuation, IComparer<T>? comparer)
        {
            _continuation = continuation;
            _comparer = comparer;
            _value = default!;
            _hasValue = false;
        }

        public void Initialize(ObserverStateMachineBox box) => _continuation.Initialize(box);
        public void Dispose() => _continuation.Dispose();

        public void OnNext(T value)
        {
            if (_hasValue)
            {
                var result =
                    _comparer?.Compare(_value, value) ??
                    Comparer<T>.Default.Compare(_value, value);
                if (result > 0)
                {
                    _value = value;
                }
            }
            else
            {
                _value = value;
                _hasValue = true;
            }
        }

        public void OnError(Exception error)
        {
            _continuation.OnError(error);
        }

        public void OnCompleted()
        {
            if (_hasValue)
            {
                _continuation.OnNext(_value);
                _continuation.OnCompleted();
            }
            else
            {
                _continuation.OnError(new InvalidOperationException());
            }
        }
    }
}