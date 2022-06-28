using System;
using System.Collections.Generic;
using Kinetic.Linq.StateMachines;

namespace Kinetic.Linq;

public static partial class Observable
{
    public static ObserverBuilder<bool> Contains<TSource>(this in ObserverBuilder<TSource> source, TSource value, IEqualityComparer<TSource>? comparer = null) =>
        source.ContinueWith<ContainsStateMachineFactory<TSource>, bool>(new(value, comparer));

    public static ObserverBuilder<bool> Contains<TSource>(this IObservable<TSource> source, TSource value, IEqualityComparer<TSource>? comparer = null) =>
        source.ToBuilder().Contains(value, comparer);

    private readonly struct ContainsStateMachineFactory<TSource> : IObserverStateMachineFactory<TSource, bool>
    {
        private readonly TSource _value;
        private readonly IEqualityComparer<TSource>? _comparer;

        public ContainsStateMachineFactory(TSource value, IEqualityComparer<TSource>? comparer)
        {
            _value = value;
            _comparer = comparer;
        }

        public void Create<TContinuation>(in TContinuation continuation, ObserverStateMachine<TSource> source)
            where TContinuation : struct, IObserverStateMachine<bool>
        {
            source.ContinueWith(new ContainsStateMachine<TContinuation, TSource>(continuation, _value, _comparer));
        }
    }

    private struct ContainsStateMachine<TContinuation, TSource> : IObserverStateMachine<TSource>
        where TContinuation : struct, IObserverStateMachine<bool>
    {
        private TContinuation _continuation;
        private TSource _value;
        private IEqualityComparer<TSource>? _comparer;

        public ContainsStateMachine(TContinuation continuation, TSource value, IEqualityComparer<TSource>? comparer)
        {
            _continuation = continuation;
            _value = value;
            _comparer = comparer;
        }

        public void Initialize(IObserverStateMachineBox box) => _continuation.Initialize(box);
        public void Dispose() => _continuation.Dispose();

        public void OnNext(TSource value)
        {
            var result =
                _comparer?.Equals(_value, value) ??
                EqualityComparer<TSource>.Default.Equals(_value, value);
            if (result)
            {
                _continuation.OnNext(true);
                _continuation.OnCompleted();
            }
        }

        public void OnError(Exception error)
        {
            _continuation.OnError(error);
        }

        public void OnCompleted()
        {
            _continuation.OnNext(false);
            _continuation.OnCompleted();
        }
    }
}