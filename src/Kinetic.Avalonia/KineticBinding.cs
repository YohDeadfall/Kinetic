using System;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Interactivity;
using Kinetic.Linq;
using Kinetic.Runtime;

namespace Kinetic.Data;

public static class KineticBinding
{
    private const string TwoWayCollectionBindingsNotSupported = "Two way collection bindings are not supported.";

    private static IObservable<object?> GetDataContext(AvaloniaObject target) =>
        target.GetObservable(StyledElement.DataContextProperty);

    public static IDisposable BindTwoWay<T, TOperator>(
        this AvaloniaObject target,
        AvaloniaProperty property,
        Func<IObservable<object?>, Operator<TOperator, Property<T>?>> expression)
        where TOperator : IOperator<Property<T>?>
    {
        return target.Bind(
            property,
            expression
                .Invoke(GetDataContext(target))
                .Build<IBox<T>, BoxFactory<T>, StateMachine<T>>(
                    new(target, property),
                    new()));
    }

    public static IDisposable BindOneWay<T, TOperator>(
        this AvaloniaObject target,
        AvaloniaProperty property,
        Func<IObservable<object?>, Operator<TOperator, Property<T>?>> expression)
        where TOperator : IOperator<Property<T>?>
    {
        return target.Bind(
            property,
            expression
                .Invoke(GetDataContext(target))
                .Build<IBox<T>, BoxFactory<T>, StateMachine<T>.OneWay>(
                    new(target, property),
                    new()));
    }

    public static IDisposable BindOneWay<T, TOperator>(
        this AvaloniaObject target,
        AvaloniaProperty property,
        Func<IObservable<object?>, Operator<TOperator, ReadOnlyProperty<T>?>> expression)
        where TOperator : IOperator<ReadOnlyProperty<T>?>
    {
        return target.Bind(
            property,
            expression
                .Invoke(GetDataContext(target))
                .Build<IBox<T>, BoxFactory<T>, StateMachine<T>>(
                    new(target, property),
                    new()));
    }

    private interface IBox<TProperty> : IDisposable, IObservable<object?>
    {
        void Initialize(ref StateMachine<TProperty> publisher);
    }

    private sealed class Box<TContext, TProperty, TStateMachine> : StateMachineBox<TContext, TStateMachine>, IBox<TProperty>
        where TStateMachine : struct, IEntryStateMachine<TContext>
    {
        private readonly AvaloniaObject _targetObject;
        private readonly AvaloniaProperty _targetProperty;
        private StateMachineValueReference<TProperty, StateMachine<TProperty>> _publisher;

        public Box(TStateMachine stateMachine, AvaloniaObject targetObject, AvaloniaProperty targetProperty) :
            base(stateMachine)
        {
            _targetObject = targetObject;
            _targetProperty = targetProperty;

            StateMachine.Initialize(this);
        }

        public void Initialize(ref StateMachine<TProperty> publisher) =>
            _publisher = StateMachineValueReference<TProperty>.Create(ref publisher);

        public IDisposable Subscribe(IObserver<object?> observer)
        {
            ref var publisher = ref _publisher.Target;

            publisher.Observer = publisher.Observer is { }
                ? throw new InvalidOperationException()
                : observer;

            if (_targetObject.GetValue(Interactive.DataContextProperty) is TContext context)
            {
                StateMachine.OnNext(context);
            }

            _targetObject.PropertyChanged += TargetPropertyChanged;

            return this;
        }

        public void Dispose()
        {
            _targetObject.PropertyChanged -= TargetPropertyChanged;

            StateMachine.Dispose();
        }

        private void TargetPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs args)
        {
            if (_targetProperty == args.Property &&
                _publisher.Target.Property is { } sourceProperty)
            {
                sourceProperty.Set(args.GetNewValue<TProperty>());
            }
        }
    }

    private readonly struct BoxFactory<TProperty> : IStateMachineBoxFactory<IBox<TProperty>>
    {
        private readonly AvaloniaObject _targetObject;
        private readonly AvaloniaProperty _targetProperty;

        public BoxFactory(AvaloniaObject targetObject, AvaloniaProperty targetProperty)
        {
            _targetObject = targetObject;
            _targetProperty = targetProperty;
        }

        public IBox<TProperty> Create<TSource, TStateMachine>(TStateMachine stateMachine)
            where TStateMachine : struct, IEntryStateMachine<TSource>
        {
            return new Box<TSource, TProperty, TStateMachine>(stateMachine, _targetObject, _targetProperty);
        }
    }

    [StructLayout(LayoutKind.Auto)]
    private struct StateMachine<TProperty> :
        IStateMachine<Property<TProperty>?>,
        IStateMachine<ReadOnlyProperty<TProperty>?>,
        IStateMachine<TProperty>
    {
        private StateMachineBox? _box;
        private IDisposable? _subscription;

        public Property<TProperty>? Property { get; private set; }
        public IObserver<object?>? Observer { get; set; }

        public StateMachineBox Box =>
            _box ?? throw new InvalidOperationException();

        StateMachineReference<Property<TProperty>?> IStateMachine<Property<TProperty>?>.Reference =>
            StateMachineReference<Property<TProperty>?>.Create(ref this);

        StateMachineReference<ReadOnlyProperty<TProperty>?> IStateMachine<ReadOnlyProperty<TProperty>?>.Reference =>
            StateMachineReference<ReadOnlyProperty<TProperty>?>.Create(ref this);

        public StateMachineReference<TProperty> Reference =>
            StateMachineReference<TProperty>.Create(ref this);

        public StateMachineReference? Continuation =>
            null;

        public void Initialize(StateMachineBox box)
        {
            _box = box;

            ((IBox<TProperty>) box).Initialize(ref this);
        }

        public void Dispose()
        {
            _subscription?.Dispose();
            _subscription = null;

            Property = null;
            Observer = null;
        }

        public void OnNext(Property<TProperty>? value) =>
            OnNextCore(Property = value);

        public void OnNext(ReadOnlyProperty<TProperty>? value) =>
            OnNextCore(value);

        private void OnNextCore(ReadOnlyProperty<TProperty>? value)
        {
            try
            {
                _subscription?.Dispose();

                if (value is { } property)
                {
                    _subscription = property.Changed.Subscribe(Reference);
                }
                else
                {
                    _subscription = null;
                    Observer?.OnNext(default!);
                }
            }
            catch (Exception error)
            {
                Observer?.OnError(error);
            }
        }

        public void OnError(Exception error) =>
            Observer?.OnError(error);

        public void OnCompleted() =>
            Observer?.OnCompleted();

        void IObserver<TProperty>.OnNext(TProperty value) =>
            Observer?.OnNext(value);

        [StructLayout(LayoutKind.Auto)]
        public struct OneWay : IStateMachine<Property<TProperty>?>
        {
            private StateMachine<TProperty> _inner;

            public StateMachineBox Box =>
                _inner.Box;

            public StateMachineReference<Property<TProperty>?> Reference =>
                StateMachineReference<Property<TProperty>?>.Create(ref _inner);

            public StateMachineReference? Continuation =>
                _inner.Continuation;

            public void Dispose() =>
                _inner.Dispose();

            public void Initialize(StateMachineBox box) =>
                _inner.Initialize(box);

            public void OnCompleted() =>
                _inner.OnCompleted();

            public void OnError(Exception error) =>
                _inner.OnError(error);

            public void OnNext(Property<TProperty>? value) =>
                _inner.OnNext((ReadOnlyProperty<TProperty>?) value);
        }
    }
}