using System;
using System.Linq;
using Kinetic.Linq;
using Kinetic.Linq.StateMachines;

namespace Kinetic.Data;

public static class KineticBindingPath
{
    public static ObserverBuilder<Property<TResult>?> Property<TSource, TResult>(this ObserverBuilder<TSource> source, Func<TSource, Property<TResult>?> selector) =>
        source.Select(selector);

    public static ObserverBuilder<Property<TResult>?> Property<TSource, TResult>(this ObserverBuilder<Property<TSource>?> source, Func<TSource?, Property<TResult>?> selector) =>
        source.ContinueWith<PropertyStateMachineFactory<TSource>, TSource?>(default).Property(selector);

    public static ObserverBuilder<Property<TResult>?> Property<TSource, TResult>(this ObserverBuilder<ReadOnlyProperty<TSource>?> source, Func<TSource?, Property<TResult>?> selector) =>
        source.ContinueWith<PropertyStateMachineFactory<TSource>, TSource?>(default).Property(selector);

    public static ObserverBuilder<ReadOnlyProperty<TResult>?> Property<TSource, TResult>(this ObserverBuilder<TSource> source, Func<TSource, ReadOnlyProperty<TResult>?> selector) =>
        source.Select(selector);

    public static ObserverBuilder<ReadOnlyProperty<TResult>?> Property<TSource, TResult>(this ObserverBuilder<Property<TSource>?> source, Func<TSource?, ReadOnlyProperty<TResult>?> selector) =>
        source.ContinueWith<PropertyStateMachineFactory<TSource>, TSource?>(default).Property(selector);

    public static ObserverBuilder<ReadOnlyProperty<TResult>?> Property<TSource, TResult>(this ObserverBuilder<ReadOnlyProperty<TSource>?> source, Func<TSource?, ReadOnlyProperty<TResult>?> selector) =>
        source.ContinueWith<PropertyStateMachineFactory<TSource>, TSource?>(default).Property(selector);

    private struct PropertyStateMachineFactory<TSource> :
        IStateMachineFactory<Property<TSource>?, TSource?>,
        IStateMachineFactory<ReadOnlyProperty<TSource>?, TSource?>
    {
        public void Create<TContinuation>(in TContinuation continuation, ObserverStateMachine<Property<TSource>?> source)
            where TContinuation : struct, IStateMachine<TSource?>
        {
            source.ContinueWith(new PropertyStateMachine<TContinuation, TSource>(continuation));
        }

        public void Create<TContinuation>(in TContinuation continuation, ObserverStateMachine<ReadOnlyProperty<TSource>?> source)
            where TContinuation : struct, IStateMachine<TSource?>
        {
            source.ContinueWith(new PropertyStateMachine<TContinuation, TSource>(continuation));
        }
    }

    private struct PropertyStateMachine<TContinuation, TSource> :
        IStateMachine<Property<TSource>?>,
        IStateMachine<ReadOnlyProperty<TSource>?>,
        IStateMachine<TSource>
        where TContinuation : struct, IStateMachine<TSource?>
    {
        private TContinuation _continuation;
        private IDisposable? _subscription;

        public PropertyStateMachine(TContinuation continuation) =>
            _continuation = continuation;

        public StateMachineBox Box =>
            _continuation.Box;

        StateMachineReference<Property<TSource>?> IStateMachine<Property<TSource>?>.Reference =>
            new StateMachineReference<Property<TSource>?, PropertyStateMachine<TContinuation, TSource>>(ref this);

        StateMachineReference<ReadOnlyProperty<TSource>?> IStateMachine<ReadOnlyProperty<TSource>?>.Reference =>
            new StateMachineReference<ReadOnlyProperty<TSource>?, PropertyStateMachine<TContinuation, TSource>>(ref this);

        StateMachineReference<TSource> IStateMachine<TSource>.Reference =>
            new StateMachineReference<TSource, PropertyStateMachine<TContinuation, TSource>>(ref this);

        StateMachineReference? IStateMachine<Property<TSource>?>.Continuation =>
            _continuation.Reference;

        StateMachineReference? IStateMachine<ReadOnlyProperty<TSource>?>.Continuation =>
            _continuation.Reference;

        StateMachineReference? IStateMachine<TSource>.Continuation =>
            null;

        public void Initialize(StateMachineBox box) =>
            _continuation.Initialize(box);

        public void Dispose()
        {
            _subscription?.Dispose();
            _subscription = null;

            _continuation.Dispose();
        }

        public void OnNext(Property<TSource>? value) => OnNextCore(value);
        public void OnNext(ReadOnlyProperty<TSource>? value) => OnNextCore(value);
        public void OnNext(TSource value) => _continuation.OnNext(value);
        public void OnCompleted() => _continuation.OnCompleted();

        public void OnError(Exception error)
        {
            _subscription?.Dispose();
            _subscription = null;

            _continuation.OnError(error);
        }

        private void OnNextCore(ReadOnlyProperty<TSource>? value)
        {
            _subscription?.Dispose();

            if (value is { } property)
            {
                _subscription = property.Changed.Subscribe(ref this);
            }
            else
            {
                _subscription = null;
                _continuation.OnNext(default);
            }
        }
    }
}