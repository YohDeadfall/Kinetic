using System;
using Kinetic.Linq.StateMachines;

namespace Kinetic.Linq;

public static partial class Observable
{
    public static ObserverBuilder<TSource?> DefaultIfEmpty<TSource>(this ObserverBuilder<TSource> source) =>
        source.DefaultIfEmpty(default);

    public static ObserverBuilder<TSource?> DefaultIfEmpty<TSource>(this ObserverBuilder<TSource> source, TSource? defaultValue) =>
        source.ContinueWith<DefaultIfEmptyStateMachineFactory<TSource>, TSource?>(new(defaultValue));

    public static ObserverBuilder<TSource?> DefaultIfEmpty<TSource>(this IObservable<TSource> source) =>
        source.ToBuilder().DefaultIfEmpty();

    public static ObserverBuilder<TSource?> DefaultIfEmpty<TSource>(this IObservable<TSource> source, TSource? defaultValue) =>
        source.ToBuilder().DefaultIfEmpty(defaultValue);

    private readonly struct DefaultIfEmptyStateMachineFactory<TSource> : IStateMachineFactory<TSource, TSource?>
    {
        private readonly TSource? _defaultValue;

        public DefaultIfEmptyStateMachineFactory(TSource? defaultValue) => _defaultValue = defaultValue;

        public void Create<TContinuation>(in TContinuation continuation, ObserverStateMachine<TSource> source)
            where TContinuation : struct, IStateMachine<TSource?>
        {
            source.ContinueWith(new DefaultIfEmptyStateMachine<TSource, TContinuation>(continuation, _defaultValue));
        }
    }

    private struct DefaultIfEmptyStateMachine<TSource, TContinuation> : IStateMachine<TSource>
        where TContinuation : struct, IStateMachine<TSource?>
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

        public StateMachineBox Box =>
            _continuation.Box;

        public StateMachine<TSource> Reference =>
            StateMachine<TSource>.Create(ref this);

        public StateMachine? Continuation =>
            _continuation.Reference;

        public void Initialize(StateMachineBox box) =>
            _continuation.Initialize(box);

        public void Dispose() =>
            _continuation.Dispose();

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