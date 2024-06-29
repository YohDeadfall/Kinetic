using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Kinetic.Linq.StateMachines;

namespace Kinetic.Linq;

public static partial class Observable
{
    public static ObserverBuilder<TSource> Min<TSource>(this ObserverBuilder<TSource> source, IComparer<TSource>? comparer = null) =>
        source.ContinueWith<MinStateMachineBuilder<TSource>, TSource>(new(comparer));

    public static ObserverBuilder<TSource> Min<TSource>(this IObservable<TSource> source, IComparer<TSource>? comparer = null) =>
        source.ToBuilder().Min(comparer);

    private readonly struct MinStateMachineBuilder<TSource> : IStateMachineFactory<TSource, TSource>
    {
        private readonly IComparer<TSource>? _comparer;

        public MinStateMachineBuilder(IComparer<TSource>? comparer) => _comparer = comparer;

        public void Create<TContinuation>(in TContinuation continuation, ObserverStateMachine<TSource> source)
            where TContinuation : struct, IStateMachine<TSource>
        {
            source.ContinueWith(new MinStateMachine<TSource, TContinuation>(continuation, _comparer));
        }
    }

    private struct MinStateMachine<TSource, TContinuation> : IStateMachine<TSource>
        where TContinuation : struct, IStateMachine<TSource>
    {
        private TContinuation _continuation;
        private readonly IComparer<TSource>? _comparer;

        [AllowNull]
        private TSource _value;
        private bool _hasValue;

        public MinStateMachine(TContinuation continuation, IComparer<TSource>? comparer)
        {
            _continuation = continuation;
            _comparer = comparer;
        }

        public StateMachineBox Box =>
            _continuation.Box;

        public void Initialize(StateMachineBox box) =>
            _continuation.Initialize(box);

        public void Dispose() =>
            _continuation.Dispose();

        public void OnNext(TSource value)
        {
            if (_hasValue)
            {
                var result =
                    _comparer?.Compare(_value, value) ??
                    Comparer<TSource>.Default.Compare(_value, value);

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