using System;
using System.Diagnostics;
using System.Linq;
using Kinetic.Linq;
using Kinetic.Linq.StateMachines;

namespace Kinetic.Data;

public static class KineticBindingPath
{
    public static ObserverBuilder<Property<TResult>?> Property<TSource, TResult>(this in ObserverBuilder<TSource> source, Func<TSource, Property<TResult>?> selector) =>
        source.Select(selector);

    public static ObserverBuilder<Property<TResult>?> Property<TSource, TResult>(this in ObserverBuilder<Property<TSource>?> source, Func<TSource?, Property<TResult>?> selector) =>
        source.ContinueWith<PropertyStateMachineFactory<TSource>, TSource?>(default).Property(selector);

    public static ObserverBuilder<Property<TResult>?> Property<TSource, TResult>(this in ObserverBuilder<ReadOnlyProperty<TSource>?> source, Func<TSource?, Property<TResult>?> selector) =>
        source.ContinueWith<PropertyStateMachineFactory<TSource>, TSource?>(default).Property(selector);

    public static ObserverBuilder<ReadOnlyProperty<TResult>?> Property<TSource, TResult>(this in ObserverBuilder<TSource> source, Func<TSource, ReadOnlyProperty<TResult>?> selector) =>
        source.Select(selector);

    public static ObserverBuilder<ReadOnlyProperty<TResult>?> Property<TSource, TResult>(this in ObserverBuilder<Property<TSource>?> source, Func<TSource?, ReadOnlyProperty<TResult>?> selector) =>
        source.ContinueWith<PropertyStateMachineFactory<TSource>, TSource?>(default).Property(selector);

    public static ObserverBuilder<ReadOnlyProperty<TResult>?> Property<TSource, TResult>(this in ObserverBuilder<ReadOnlyProperty<TSource>?> source, Func<TSource?, ReadOnlyProperty<TResult>?> selector) =>
        source.ContinueWith<PropertyStateMachineFactory<TSource>, TSource?>(default).Property(selector);

    private struct PropertyStateMachineFactory<TSource>
        : IObserverStateMachineFactory<Property<TSource>?, TSource?>
        , IObserverStateMachineFactory<ReadOnlyProperty<TSource>?, TSource?>
    {
        public void Create<TContinuation>(in TContinuation continuation, ObserverStateMachine<Property<TSource>?> source)
            where TContinuation : struct, IObserverStateMachine<TSource?>
        {
            source.ContinueWith(new PropertyStateMachine<TContinuation, TSource>(continuation));
        }

        public void Create<TContinuation>(in TContinuation continuation, ObserverStateMachine<ReadOnlyProperty<TSource>?> source)
            where TContinuation : struct, IObserverStateMachine<TSource?>
        {
            source.ContinueWith(new PropertyStateMachine<TContinuation, TSource>(continuation));
        }
    }

    private struct PropertyStateMachine<TContinuation, TSource>
        : IObserverStateMachine<Property<TSource>?>
        , IObserverStateMachine<ReadOnlyProperty<TSource>?>
        , IObserverStateMachine<TSource>
        where TContinuation : struct, IObserverStateMachine<TSource?>
    {
        private TContinuation _continuation;
        private ObserverStateMachineBox? _box;
        private IDisposable? _subscription;

        public PropertyStateMachine(TContinuation continuation)
        {
            _continuation = continuation;

            _box = null;
            _subscription = null;
        }

        public void Initialize(ObserverStateMachineBox box)
        {
            _box = box;
            _continuation.Initialize(box);
        }

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
            Debug.Assert(_box is not null);

            _subscription?.Dispose();

            if (value is { } property)
            {
                _subscription = _box.Subscribe(
                    property.Changed, this);
            }
            else
            {
                _subscription = null;
                _continuation.OnNext(default);
            }
        }
    }
}