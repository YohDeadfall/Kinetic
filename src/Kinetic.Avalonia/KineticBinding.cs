using System;
using Avalonia;
using Kinetic.Linq;
using Kinetic.Linq.StateMachines;

namespace Kinetic.Data;

public delegate ObserverBuilder<T> BindingExpressionFactory<T>(ObserverBuilder<object?> source);

public static class KineticBinding
{
    private const string TwoWayCollectionBindingsNotSupported = "Two way collection bindings are not supported.";

    private static ObserverBuilder<object?> GetDataContext(AvaloniaObject target) =>
        target.GetObservable(StyledElement.DataContextProperty).ToBuilder();

    public static IDisposable BindOneWay<T>(this AvaloniaObject target, AvaloniaProperty property, BindingExpressionFactory<Property<T>?> expression) =>
        target.Bind(
            property,
            expression
                .Invoke(GetDataContext(target))
                .ContinueWith<PropertyStateMachineFactory<T>, T?>(default)
                .Build<PublishStateMachine<T?>, BoxFactory<T?>, IBox>(continuation: new(), factory: new())
                .ToBinding());

    public static IDisposable BindOneWay<T>(this AvaloniaObject target, AvaloniaProperty property, BindingExpressionFactory<ReadOnlyProperty<T>?> expression) =>
        target.Bind(
            property,
            expression
                .Invoke(GetDataContext(target))
                .ContinueWith<PropertyStateMachineFactory<T>, T?>(default)
                .Build<PublishStateMachine<T?>, BoxFactory<T?>, IBox>(continuation: new(), factory: new())
                .ToBinding());

    public static IDisposable BindTwoWay<T>(this AvaloniaObject target, AvaloniaProperty property, BindingExpressionFactory<Property<T>?> expression) =>
        target.Bind(
            property,
            expression
                .Invoke(GetDataContext(target))
                .ContinueWith<PropertyStateMachineFactory<T>, T?>(default)
                .Build<PublishStateMachine<T?>, BoxFactory<T?>, IBox>(continuation: new(), factory: new(target, property)));

    internal interface IBox : IDisposable, IObservable<object?>
    {
        IObserver<object?>? Observer { get; }
    }

    internal interface IBox<TProperty> : IBox
    {
        Property<TProperty>? Property { get; set; }
    }

    private sealed class Box<TContext, TProperty, TStateMachine> : StateMachineBox<TContext, TStateMachine>, IBox<TProperty>
        where TStateMachine : struct, IStateMachine<TContext>
    {
        private readonly AvaloniaObject? _targetObject;
        private readonly AvaloniaProperty? _targetProperty;

        private IObserver<object?>? _observer;

        public Property<TProperty>? Property { get; set; }
        public IObserver<object?>? Observer => _observer;

        public Box(in TStateMachine stateMachine, AvaloniaObject? targetObject, AvaloniaProperty? targetProperty) :
            base(stateMachine)
        {
            _targetObject = targetObject;
            _targetProperty = targetProperty;

            StateMachine.Initialize(this);
        }

        public IDisposable Subscribe(IObserver<object?> observer)
        {
            _observer = _observer is { }
                ? throw new InvalidOperationException()
                : observer;

            _observer.OnNext(Property is { } property ? property.Get() : null);

            if (_targetObject is { } &&
                _targetProperty is { })
            {
                _targetObject.PropertyChanged += TargetPropertyChanged;
            }

            return this;
        }

        public void Dispose()
        {
            if (_observer is { })
            {
                if (_targetObject is { } &&
                    _targetProperty is { })
                {
                    _targetObject.PropertyChanged -= TargetPropertyChanged;
                }

                StateMachine.Dispose();
            }
        }

        private void TargetPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs args)
        {
            if (args.Property == _targetProperty && Property is { } sourceProperty)
            {
                sourceProperty.Set(args.GetNewValue<TProperty>());
            }
        }
    }

    internal readonly struct BoxFactory<TProperty> : IStateMachineBoxFactory<IBox>
    {
        private readonly AvaloniaObject? _targetObject;
        private readonly AvaloniaProperty? _targetProperty;

        public BoxFactory() :
            this(null, null)
        { }

        public BoxFactory(AvaloniaObject? targetObject, AvaloniaProperty? targetProperty)
        {
            _targetObject = targetObject;
            _targetProperty = targetProperty;
        }

        public IBox Create<T, TStateMachine>(in TStateMachine stateMachine)
            where TStateMachine : struct, IStateMachine<T> =>
            new Box<T, TProperty, TStateMachine>(stateMachine, _targetObject, _targetProperty);
    }

    internal struct PublishStateMachine<TProperty> : IStateMachine<TProperty>
    {
        private IBox? _box;

        public StateMachineBox Box =>
            (StateMachineBox) (_box ?? throw new InvalidOperationException());

        public StateMachine<TProperty> Reference =>
            new StateMachine<TProperty, PublishStateMachine<TProperty>>(ref this);

        public StateMachine? Continuation =>
            null;

        public void Initialize(StateMachineBox box) =>
            _box = (IBox) box;

        public void Dispose() =>
            _box = null;

        public void OnCompleted() =>
            _box!.Observer?.OnCompleted();

        public void OnError(Exception error) =>
            _box!.Observer?.OnError(error);

        public void OnNext(TProperty value) =>
            _box!.Observer?.OnNext(value);
    }

    internal struct PropertyStateMachine<TContinuation, TProperty> :
        IStateMachine<Property<TProperty>?>,
        IStateMachine<ReadOnlyProperty<TProperty>?>,
        IStateMachine<TProperty>
        where TContinuation : struct, IStateMachine<TProperty?>
    {
        private TContinuation _continuation;
        private IBox<TProperty>? _box;
        private IDisposable? _subscription;

        public PropertyStateMachine(in TContinuation continuation)
        {
            _continuation = continuation;

            _box = null;
            _subscription = null;
        }

        public StateMachineBox Box =>
            _continuation.Box;

        StateMachine<Property<TProperty>?> IStateMachine<Property<TProperty>?>.Reference =>
            new StateMachine<Property<TProperty>?, PropertyStateMachine<TContinuation, TProperty>>(ref this);

        StateMachine<ReadOnlyProperty<TProperty>?> IStateMachine<ReadOnlyProperty<TProperty>?>.Reference =>
            new StateMachine<ReadOnlyProperty<TProperty>?, PropertyStateMachine<TContinuation, TProperty>>(ref this);

        StateMachine<TProperty> IStateMachine<TProperty>.Reference =>
            new StateMachine<TProperty, PropertyStateMachine<TContinuation, TProperty>>(ref this);

        StateMachine? IStateMachine<Property<TProperty>?>.Continuation =>
            _continuation.Reference;

        StateMachine? IStateMachine<ReadOnlyProperty<TProperty>?>.Continuation =>
            _continuation.Reference;

        StateMachine? IStateMachine<TProperty>.Continuation =>
            null;

        public void Initialize(StateMachineBox box)
        {
            _box = (IBox<TProperty>) box;
            _continuation.Initialize(box);
        }

        public void Dispose()
        {
            _continuation.Dispose();

            _box!.Property = null;
            _box = null;

            _subscription?.Dispose();
            _subscription = null;
        }

        public void OnNext(Property<TProperty>? value) =>
            OnNextCore(_box!.Property = value);

        public void OnNext(ReadOnlyProperty<TProperty>? value) =>
            OnNextCore(value);

        private void OnNextCore(ReadOnlyProperty<TProperty>? value)
        {
            try
            {
                _subscription?.Dispose();

                if (value is { } property)
                {
                    _subscription = property.Changed.Subscribe(ref this);
                }
                else
                {
                    _subscription = null;
                    _continuation.OnNext(default!);
                }
            }
            catch (Exception error)
            {
                _continuation.OnError(error);
            }
        }

        public void OnError(Exception error) =>
            _continuation.OnError(error);

        public void OnCompleted() =>
            _continuation.OnCompleted();

        void IObserver<TProperty>.OnNext(TProperty value) =>
            _continuation.OnNext(value);
    }

    internal readonly struct PropertyStateMachineFactory<TValue> :
        IStateMachineFactory<Property<TValue>?, TValue?>,
        IStateMachineFactory<ReadOnlyProperty<TValue>?, TValue?>
    {
        public void Create<TContinuation>(in TContinuation continuation, ObserverStateMachine<Property<TValue>?> source)
            where TContinuation : struct, IStateMachine<TValue?> =>
            source.ContinueWith(new PropertyStateMachine<TContinuation, TValue>(continuation));

        public void Create<TContinuation>(in TContinuation continuation, ObserverStateMachine<ReadOnlyProperty<TValue>?> source)
            where TContinuation : struct, IStateMachine<TValue?> =>
            source.ContinueWith(new PropertyStateMachine<TContinuation, TValue>(continuation));
    }
}