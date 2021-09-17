using System;
using System.Collections.Generic;

namespace Kinetic.Linq
{
    public static partial class Observable
    {
        public static MinBuilder<ObserverBuilder<T>, T> Min<T>(this IObservable<T> observable, IComparer<T>? comparer = null) =>
            observable.ToBuilder().Min(comparer);

        public static MinBuilder<TObservable, T> Min<TObservable, T>(this TObservable observable, IComparer<T>? comparer = null)
            where TObservable : struct, IObserverBuilder<T> =>
            new(observable, comparer);
    }

    public readonly struct MinBuilder<TObservable, T> : IObserverBuilder<T>
        where TObservable : struct, IObserverBuilder<T>
    {
        private readonly TObservable _observable;
        private readonly IComparer<T>? _comparer;

        public MinBuilder(in TObservable observable, IComparer<T>? comparer)
        {
            _observable = observable;
            _comparer = comparer;
        }

        public void Build<TStateMachine, TFactory>(in TStateMachine stateMachine, ref TFactory factory)
            where TStateMachine : struct, IObserverStateMachine<T>
            where TFactory : struct, IObserverFactory
        {
            _observable.Build(
                stateMachine: new MinStateMachine<TStateMachine, T>(stateMachine, _comparer),
                ref factory);
        }

        public void BuildWithFactory<TStateMachine, TFactory>(in TStateMachine stateMachine, ref TFactory factory)
            where TStateMachine : struct, IObserverStateMachineFactory
            where TFactory : struct, IObserverFactory
        {
            stateMachine.Create<T, MinBuilder<TObservable, T>, TFactory>(this, ref factory);
        }
    }

    public struct MinStateMachine<TContinuation, T> : IObserverStateMachine<T>
        where TContinuation : IObserverStateMachine<T>
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

        public void Initialize(IObserverStateMachineBox box) => _continuation.Initialize(box);
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