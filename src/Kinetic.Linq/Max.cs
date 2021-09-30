using System;
using System.Collections.Generic;

namespace Kinetic.Linq
{
    public static partial class Observable
    {
        public static ObserverBuilder<TSource> Max<TSource>(this in ObserverBuilder<TSource> source, IComparer<TSource>? comparer = null) =>
            source.ContinueWith<TSource, MaxStateMachineBuilder<TSource>>(new(comparer));

        public static ObserverBuilder<TSource> Max<TSource>(this IObservable<TSource> source, IComparer<TSource>? comparer = null) =>
            source.ToBuilder().Max(comparer);
    }

    internal readonly struct MaxStateMachineBuilder<TSource> : IObserverStateMachineFactory<TSource, TSource>
    {
        private readonly IComparer<TSource>? _comparer;

        public MaxStateMachineBuilder(IComparer<TSource>? comparer) => _comparer = comparer;

        public void Create<TContinuation>(in TContinuation continuation, ObserverStateMachine<TSource> source)
            where TContinuation : struct, IObserverStateMachine<TSource>
        {
            source.ContinueWith(new MaxStateMachine<TContinuation, TSource>(continuation, _comparer));
        }
    }

    internal struct MaxStateMachine<TContinuation, TSource> : IObserverStateMachine<TSource>
        where TContinuation : IObserverStateMachine<TSource>
    {
        private TContinuation _continuation;
        private IComparer<TSource>? _comparer;
        private TSource _value;
        private bool _hasValue;

        public MaxStateMachine(TContinuation continuation, IComparer<TSource>? comparer)
        {
            _continuation = continuation;
            _comparer = comparer;
            _value = default!;
            _hasValue = false;
        }

        public void Initialize(IObserverStateMachineBox box) => _continuation.Initialize(box);
        public void Dispose() => _continuation.Dispose();

        public void OnNext(TSource value)
        {
            if (_hasValue)
            {
                var result =
                    _comparer?.Compare(_value, value) ??
                    Comparer<TSource>.Default.Compare(_value, value);
                if (result < 0)
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