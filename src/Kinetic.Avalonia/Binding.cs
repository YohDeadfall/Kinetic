using System;
using System.Diagnostics;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Data;
using Kinetic.Linq;
using Kinetic.Linq.StateMachines;

namespace Kinetic.Data
{
    public delegate ObserverBuilder<T> BindingExpressionFactory<T>(ObserverBuilder<object?> source);

    public abstract class Binding : IBinding
    {
        public abstract InstancedBinding Initiate(
            IAvaloniaObject target,
            AvaloniaProperty targetProperty,
            object? anchor = null,
            bool enableDataValidation = false);

        protected InstancedBinding Initiate<T>(IAvaloniaObject target, BindingExpressionFactory<Property<T>?> expression) =>
            InstancedBinding.TwoWay(expression.Invoke(default).Build<ExpressionStateMachine<T>, ExpressionFactory<T>, Expression<T>>(continuation: new(), factory: new(target)));

        protected InstancedBinding Initiate<T>(IAvaloniaObject target, BindingExpressionFactory<ReadOnlyProperty<T>?> expression) =>
            InstancedBinding.OneWay(expression.Invoke(default).Build<ExpressionStateMachine<T>, ExpressionFactory<T>, Expression<T>>(continuation: new(), factory: new(target)));

        public static Binding OneWay<T>(BindingExpressionFactory<ReadOnlyProperty<T>?> expression) =>
            new OneWayBinding<T>(expression);

        public static Binding TwoWay<T>(BindingExpressionFactory<Property<T>?> expression) =>
            new TwoWayBinding<T>(expression);
    }

    public static class BindingPath
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
            private IObserverStateMachineBox? _box;
            private IDisposable? _subscription;

            public PropertyStateMachine(TContinuation continuation)
            {
                _continuation = continuation;

                _box = null;
                _subscription = null;
            }

            public void Initialize(IObserverStateMachineBox box)
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

    internal sealed class OneWayBinding<T> : Binding
    {
        public readonly BindingExpressionFactory<ReadOnlyProperty<T>?> Expression;
        public OneWayBinding(BindingExpressionFactory<ReadOnlyProperty<T>?> expression) =>
            Expression = expression;

        public override InstancedBinding Initiate(
            IAvaloniaObject target,
            AvaloniaProperty targetProperty,
            object? anchor = null,
            bool enableDataValidation = false)
        {
            return Initiate(target, Expression);
        }
    }

    internal sealed class TwoWayBinding<T> : Binding
    {
        public readonly BindingExpressionFactory<Property<T>?> Expression;
        public TwoWayBinding(BindingExpressionFactory<Property<T>?> expression) =>
            Expression = expression;

        public override InstancedBinding Initiate(
            IAvaloniaObject target,
            AvaloniaProperty targetProperty,
            object? anchor = null,
            bool enableDataValidation = false)
        {
            return Initiate(target, Expression);
        }
    }

    internal readonly struct ExpressionFactory<TResult> : IObserverFactory<Expression<TResult>>
    {
        public readonly IAvaloniaObject Target;
        public ExpressionFactory(IAvaloniaObject target) =>
            Target = target;

        public Expression<TResult> Create<TSource, TStateMachine>(in TStateMachine stateMachine)
            where TStateMachine : struct, IObserverStateMachine<TSource> =>
            new Expression<TResult, TSource, TStateMachine>(Target, stateMachine);
    }

    internal abstract class Expression<TResult> : ISubject<object?>, IObserverStateMachineBox
    {
        internal Property<TResult>? Property;
        internal IObserver<object?>? Observer;
        internal IAvaloniaObject Target;
        internal EventHandler<AvaloniaPropertyChangedEventArgs>? TargetChanged;

        public Expression(IAvaloniaObject target) =>
            Target = target;

        public abstract IDisposable Subscribe<T, TStateMachinePart>(IObservable<T> observable, in TStateMachinePart stateMachine)
            where TStateMachinePart : struct, IObserverStateMachine<T>;

        public abstract IDisposable Subscribe(IObserver<object?> observer);
        public abstract void Dispose();

        public void Next(TResult? value)
        {
            Debug.Assert(Observer is not null);
            Observer.OnNext(value);
        }

        public void Error(Exception error)
        {
            Debug.Assert(Observer is not null);
            Observer.OnNext(null);
        }

        void IObserver<object?>.OnNext(object? value)
        {
            if (Property is { } property)
            {
                property.Set((TResult) value!);
            }
        }

        void IObserver<object?>.OnError(Exception error) { }
        void IObserver<object?>.OnCompleted() { }
    }

    internal sealed class Expression<TResult, TSource, TStateMachine> : Expression<TResult>
        where TStateMachine : IObserverStateMachine<TSource>
    {
        private TStateMachine _stateMachine;

        public Expression(IAvaloniaObject target, in TStateMachine stateMachine)
            : base(target) => _stateMachine = stateMachine;

        public override IDisposable Subscribe<T, TStateMachinePart>(IObservable<T> observable, in TStateMachinePart stateMachine)
        {
            return observable.Subscribe(
                state: (self: this, offset: GetStateMachineOffset(stateMachine)),
                onNext: static (state, value) =>
                {
                    state.self
                        .GetStateMachine<TStateMachinePart>(state.offset)
                        .OnNext(value);
                },
                onError: static (state, error) =>
                {
                    state.self
                        .GetStateMachine<TStateMachinePart>(state.offset)
                        .OnError(error);
                },
                onCompleted: static (state) =>
                {
                    state.self
                        .GetStateMachine<TStateMachinePart>(state.offset)
                        .OnCompleted();
                });
        }

        private ref TStateMachinePart GetStateMachine<TStateMachinePart>(IntPtr offset)
        {
            ref var stateMachine = ref Unsafe.As<TStateMachine, IntPtr>(ref _stateMachine);
            ref var stateMachinePart = ref Unsafe.As<IntPtr, TStateMachinePart>(
                ref Unsafe.AddByteOffset(ref stateMachine, offset));
            return ref stateMachinePart!;
        }

        private IntPtr GetStateMachineOffset<TStateMachinePart>(in TStateMachinePart stateMachine)
        {
            return Unsafe.ByteOffset(
                ref Unsafe.As<TStateMachine, IntPtr>(ref _stateMachine),
                ref Unsafe.As<TStateMachinePart, IntPtr>(ref Unsafe.AsRef(stateMachine)));
        }

        public override IDisposable Subscribe(IObserver<object?> observer)
        {
            TargetChanged = TargetChanged is not null
                ? throw new InvalidOperationException()
                : (sender, args) =>
                {
                    if (args.Property == StyledElement.DataContextProperty)
                    {
                        _stateMachine.OnNext((TSource) args.NewValue!);
                    }
                };

            Observer = observer;
            Target.PropertyChanged += TargetChanged;

            try
            {
                _stateMachine.Initialize(this);
                _stateMachine.OnNext((TSource) Target.GetValue(StyledElement.DataContextProperty)!);
            }
            catch
            {
                _stateMachine.Dispose();
                throw;
            }

            return this;
        }

        public override void Dispose()
        {
            Observer = null;
            Target.PropertyChanged -= TargetChanged;

            _stateMachine.Dispose();
        }
    }

    internal struct ExpressionStateMachine<TSource>
        : IObserverStateMachine<Property<TSource>?>
        , IObserverStateMachine<ReadOnlyProperty<TSource>?>
        , IObserverStateMachine<TSource>
    {
        private Expression<TSource>? _expression;
        private IDisposable? _subscription;

        public void Initialize(IObserverStateMachineBox box)
        {
            _expression = (Expression<TSource>) box;
        }

        public void Dispose()
        {
            Debug.Assert(_expression is not null);

            _expression.Property = null;

            _subscription?.Dispose();
            _subscription = null;
        }

        public void OnNext(Property<TSource>? value) => OnNextCore(_expression!.Property = value);
        public void OnNext(ReadOnlyProperty<TSource>? value) => OnNextCore(value);

        public void OnError(Exception error)
        {
            Debug.Assert(_expression is not null);

            _subscription?.Dispose();
            _subscription = null;

            _expression.Property = null;
            _expression.Error(error);
        }

        public void OnCompleted()
        {
        }

        void IObserver<TSource>.OnNext(TSource value) => _expression!.Next(value);
        void IObserver<TSource>.OnError(Exception error) => _expression!.Error(error);
        void IObserver<TSource>.OnCompleted() { }

        private void OnNextCore(ReadOnlyProperty<TSource>? value)
        {
            Debug.Assert(_expression is not null);
            try
            {
                _subscription?.Dispose();

                if (value is { } property)
                {
                    _subscription = _expression.Subscribe(
                        property.Changed, this);
                }
                else
                {
                    _subscription = null;
                    _expression.Next(default);
                }
            }
            catch (Exception error)
            {
                _expression.Error(error);
            }
        }
    }
}