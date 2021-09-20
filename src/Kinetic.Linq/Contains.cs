using System;
using System.Collections.Generic;

namespace Kinetic.Linq
{
    public static partial class Observable
    {
        public static ContainsBuilder<ObserverBuilder<TSource>, TSource> Contains<TSource>(this IObservable<TSource> observable, TSource value, IEqualityComparer<TSource>? comparer = null) =>
            observable.ToBuilder().Contains<ObserverBuilder<TSource>, TSource>(value, comparer);

        public static ContainsBuilder<TObservable, TSource> Contains<TObservable, TSource>(this TObservable observable, TSource value, IEqualityComparer<TSource>? comparer = null)
            where TObservable : struct, IObserverBuilder<TSource> =>
            new(observable, value, comparer);
    }

    public readonly struct ContainsBuilder<TObservable, TSource> : IObserverBuilder<bool>
        where TObservable : struct, IObserverBuilder<TSource>
    {
        private readonly TObservable _observable;
        private readonly TSource _value;
        private readonly IEqualityComparer<TSource>? _comparer;

        public ContainsBuilder(in TObservable observable, TSource value, IEqualityComparer<TSource>? comparer)
        {
            _observable = observable;
            _value = value;
            _comparer = comparer;
        }

        public void Build<TStateMachine, TFactory>(in TStateMachine stateMachine, ref TFactory factory)
            where TStateMachine : struct, IObserverStateMachine<bool>
            where TFactory : struct, IObserverFactory
        {
            _observable.Build(
                stateMachine: new ContainsStateMachine<TStateMachine, TSource>(stateMachine, _value, _comparer),
                ref factory);
        }

        public void BuildWithFactory<TStateMachine, TFactory>(in TStateMachine stateMachine, ref TFactory factory)
            where TStateMachine : struct, IObserverStateMachineFactory
            where TFactory : struct, IObserverFactory
        {
            stateMachine.Create<bool, ContainsBuilder<TObservable, TSource>, TFactory>(this, ref factory);
        }
    }

    public struct ContainsStateMachine<TContinuation, TSource> : IObserverStateMachine<TSource>
        where TContinuation : IObserverStateMachine<bool>
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