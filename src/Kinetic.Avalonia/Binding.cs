using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Data;
using Kinetic.Linq;
using Kinetic.Linq.StateMachines;

namespace Kinetic.Data;

public delegate ObserverBuilder<T> BindingExpressionFactory<T>(ObserverBuilder<object?> source);

public abstract class Binding : IBinding
{
    public abstract InstancedBinding Initiate(
        IAvaloniaObject target,
        AvaloniaProperty? targetProperty,
        object? anchor = null,
        bool enableDataValidation = false);

    private protected static InstancedBinding InitiateOneWay<T>(IAvaloniaObject target, BindingExpressionFactory<Property<T>?> expression) =>
        InstancedBinding.OneWay(expression.Invoke(default).Build<ExpressionStateMachine<T>, ExpressionFactory<T>, Expression<T>>(
            continuation: new(), factory: new(target)));

    private protected static InstancedBinding InitiateOneWay<T>(IAvaloniaObject target, BindingExpressionFactory<ReadOnlyProperty<T>?> expression) =>
        InstancedBinding.OneWay(expression.Invoke(default).Build<ExpressionStateMachine<T>, ExpressionFactory<T>, Expression<T>>(
            continuation: new(), factory: new(target)));

    private protected static InstancedBinding InitiateOneWay<T>(IAvaloniaObject target, BindingExpressionFactory<Property<ObservableList<T>?>?> expression) =>
        InstancedBinding.OneWay(expression.Invoke(default).Build<CollectionExpressionStateMachine<T>, CollectionExpressionFactory<T>, CollectionExpression<T>>(
            continuation: new(), factory: new(target)));

    private protected static InstancedBinding InitiateOneWay<T>(IAvaloniaObject target, BindingExpressionFactory<ReadOnlyProperty<ObservableList<T>?>?> expression) =>
        InstancedBinding.OneWay(expression.Invoke(default).Build<CollectionExpressionStateMachine<T>, CollectionExpressionFactory<T>, CollectionExpression<T>>(
            continuation: new(), factory: new(target)));

    private protected static InstancedBinding InitiateTwoWay<T>(IAvaloniaObject target, BindingExpressionFactory<Property<T>?> expression) =>
        InstancedBinding.TwoWay(expression.Invoke(default).Build<ExpressionStateMachine<T>, ExpressionFactory<T>, Expression<T>>(
            continuation: new(), factory: new(target)));

    private protected static InstancedBinding InitiateTwoWay<T>(IAvaloniaObject target, BindingExpressionFactory<Property<ObservableList<T>?>?> expression) =>
        InstancedBinding.TwoWay(expression.Invoke(default).Build<CollectionExpressionStateMachine<T>, CollectionExpressionFactory<T>, CollectionExpression<T>>(
            continuation: new(), factory: new(target)));

    public static Binding OneWay<T>(BindingExpressionFactory<Property<T>?> expression) =>
        new OneWayBinding<T>(expression);

    public static Binding OneWay<T>(BindingExpressionFactory<ReadOnlyProperty<T>?> expression) =>
        new OneWayBinding<T>(expression);

    public static Binding OneWay<T>(BindingExpressionFactory<Property<ObservableList<T>?>?> expression) =>
        new OneWayBinding<T>(expression);

    public static Binding OneWay<T>(BindingExpressionFactory<ReadOnlyProperty<ObservableList<T>?>?> expression) =>
        new OneWayBinding<T>(expression);

    public static Binding TwoWay<T>(BindingExpressionFactory<Property<T>?> expression) =>
        new TwoWayBinding<T>(expression);

    public static Binding TwoWay<T>(BindingExpressionFactory<Property<ObservableList<T>?>?> expression) =>
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
    private readonly Delegate _expression;

    public OneWayBinding(BindingExpressionFactory<Property<T>?> expression) =>
        _expression = expression;
    public OneWayBinding(BindingExpressionFactory<ReadOnlyProperty<T>?> expression) =>
        _expression = expression;
    public OneWayBinding(BindingExpressionFactory<Property<ObservableList<T>?>?> expression) =>
        _expression = expression;
    public OneWayBinding(BindingExpressionFactory<ReadOnlyProperty<ObservableList<T>?>?> expression) =>
        _expression = expression;

    public override InstancedBinding Initiate(
        IAvaloniaObject target,
        AvaloniaProperty? targetProperty,
        object? anchor = null,
        bool enableDataValidation = false)
    {
        return _expression switch
        {
            BindingExpressionFactory<Property<T>?> expression => InitiateOneWay(target, expression),
            BindingExpressionFactory<ReadOnlyProperty<T>?> expression => InitiateOneWay(target, expression),
            BindingExpressionFactory<Property<ObservableList<T>?>?> expression => InitiateOneWay(target, expression),
            BindingExpressionFactory<ReadOnlyProperty<ObservableList<T>?>?> expression => InitiateOneWay(target, expression),
            _ => throw new NotSupportedException()
        };
    }
}

internal sealed class TwoWayBinding<T> : Binding
{
    private readonly Delegate _expression;

    public TwoWayBinding(BindingExpressionFactory<Property<T>?> expression) =>
        _expression = expression;
    public TwoWayBinding(BindingExpressionFactory<Property<ObservableList<T>?>?> expression) =>
        _expression = expression;

    public override InstancedBinding Initiate(
        IAvaloniaObject target,
        AvaloniaProperty? targetProperty,
        object? anchor = null,
        bool enableDataValidation = false)
    {
        return _expression switch
        {
            BindingExpressionFactory<Property<T>?> expression => InitiateTwoWay(target, expression),
            BindingExpressionFactory<Property<ObservableList<T>?>?> expression => InitiateTwoWay(target, expression),
            _ => throw new NotSupportedException()
        };
    }
}

internal static class Expression
{
    internal static ref TStateMachinePart GetStateMachine<TStateMachine, TStateMachinePart>(in TStateMachine stateMachine, IntPtr offset)
    {
        ref var stateMachineAddr = ref Unsafe.As<TStateMachine, IntPtr>(
            ref Unsafe.AsRef(stateMachine));
        ref var stateMachinePart = ref Unsafe.As<IntPtr, TStateMachinePart>(
            ref Unsafe.AddByteOffset(ref stateMachineAddr, offset));
        return ref stateMachinePart!;
    }

    internal static IntPtr GetStateMachineOffset<TStateMachinePart, TStateMachine>(in TStateMachine stateMachine, in TStateMachinePart stateMachinePart)
    {
        return Unsafe.ByteOffset(
            ref Unsafe.As<TStateMachine, IntPtr>(ref Unsafe.AsRef(stateMachine)),
            ref Unsafe.As<TStateMachinePart, IntPtr>(ref Unsafe.AsRef(stateMachinePart)));
    }
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
            state: (self: this, offset: Expression.GetStateMachineOffset(_stateMachine, stateMachine)),
            onNext: static (state, value) =>
            {
                Expression
                    .GetStateMachine<TStateMachine, TStateMachinePart>(state.self._stateMachine, state.offset)
                    .OnNext(value);
            },
            onError: static (state, error) =>
            {
                Expression
                    .GetStateMachine<TStateMachine, TStateMachinePart>(state.self._stateMachine, state.offset)
                    .OnError(error);
            },
            onCompleted: static (state) =>
            {
                Expression
                    .GetStateMachine<TStateMachine, TStateMachinePart>(state.self._stateMachine, state.offset)
                    .OnCompleted();
            });
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

internal abstract class CollectionExpression<TElement>
    : Expression<IEnumerable<object?>>
    , INotifyCollectionChanged
    , IList
    , IList<object?>
{
    internal ObservableList<TElement>? List;
    internal NotifyCollectionChangedEventHandler? CollectionChanged;

    protected CollectionExpression(IAvaloniaObject target)
        : base(target) { }

    event NotifyCollectionChangedEventHandler INotifyCollectionChanged.CollectionChanged
    {
        add => CollectionChanged += value;
        remove => CollectionChanged -= value;
    }

    public object? this[int index]
    {
        get => EnsureList()[index];
        set => EnsureList()[index] = (TElement) value!;
    }

    public bool IsFixedSize => false;

    public bool IsReadOnly => false;

    public int Count => EnsureList().Count;

    public bool IsSynchronized => false;

    public object SyncRoot => this;

    public void Add(object? value) =>
        throw new NotSupportedException();

    public void Clear() =>
        throw new NotSupportedException();

    public bool Contains(object? value) =>
        IsCompatibleObject(value) && EnsureList().Contains((TElement) value!);

    public void CopyTo(object?[] array, int index)
    {
        var list = EnsureList();
        for (int offset = list.Count - 1; offset >= 0; offset -= 1)
        {
            array[index + offset] = list[index];
        }
    }

    public void CopyTo(Array array, int index)
    {
        var list = EnsureList();
        for (int offset = list.Count - 1; offset >= 0; offset -= 1)
        {
            array.SetValue(list[index], index + offset);
        }
    }

    IEnumerator<object?> IEnumerable<object?>.GetEnumerator() =>
        EnsureList() is var list && list is IEnumerable<object?> enumerable
        ? enumerable.GetEnumerator()
        : list.Cast<object?>().GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() =>
        EnsureList().GetEnumerator();

    public int IndexOf(object? value) =>
        IsCompatibleObject(value) ? EnsureList().IndexOf((TElement) value!) : -1;

    public void Insert(int index, object? value) =>
        throw new NotSupportedException();

    public bool Remove(object? value) =>
        throw new NotSupportedException();
    public void RemoveAt(int index) =>
        throw new NotSupportedException();

    int IList.Add(object? value) => throw new NotSupportedException();
    void IList.Remove(object? value) => throw new NotSupportedException();

    private ObservableList<TElement> EnsureList() =>
        List ?? throw new InvalidOperationException();

    private static bool IsCompatibleObject(object? value) =>
        value is TElement ||
        value is null && default(TElement) is null;
}

internal sealed class CollectionExpression<TElement, TSource, TStateMachine> : CollectionExpression<TElement>
    where TStateMachine : IObserverStateMachine<TSource>
{
    private TStateMachine _stateMachine;

    public CollectionExpression(IAvaloniaObject target, in TStateMachine stateMachine)
        : base(target) => _stateMachine = stateMachine;

    public override IDisposable Subscribe<T, TStateMachinePart>(IObservable<T> observable, in TStateMachinePart stateMachine)
    {
        return observable.Subscribe(
            state: (self: this, offset: Expression.GetStateMachineOffset(_stateMachine, stateMachine)),
            onNext: static (state, value) =>
            {
                Expression
                    .GetStateMachine<TStateMachine, TStateMachinePart>(state.self._stateMachine, state.offset)
                    .OnNext(value);
            },
            onError: static (state, error) =>
            {
                Expression
                    .GetStateMachine<TStateMachine, TStateMachinePart>(state.self._stateMachine, state.offset)
                    .OnError(error);
            },
            onCompleted: static (state) =>
            {
                Expression
                    .GetStateMachine<TStateMachine, TStateMachinePart>(state.self._stateMachine, state.offset)
                    .OnCompleted();
            });
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

internal struct CollectionExpressionStateMachine<TElement>
    : IObserverStateMachine<Property<ObservableList<TElement>?>?>
    , IObserverStateMachine<ReadOnlyProperty<ObservableList<TElement>?>?>
    , IObserverStateMachine<ObservableList<TElement>?>
    , IObserverStateMachine<ListChange<TElement>>
{
    private CollectionExpression<TElement>? _expression;
    private IDisposable? _propertySubscription;
    private IDisposable? _listSubscription;

    public void Initialize(IObserverStateMachineBox box) =>
        _expression = (CollectionExpression<TElement>) box;

    public void Dispose()
    {
        Debug.Assert(_expression is not null);

        _expression.Property = null;
        _expression.List = null;
        _expression = null;

        _propertySubscription?.Dispose();
        _propertySubscription = null;

        _listSubscription?.Dispose();
        _listSubscription = null;
    }

    public void OnNext(Property<ObservableList<TElement>?>? value) => OnNextCore(value);
    public void OnNext(ReadOnlyProperty<ObservableList<TElement>?>? value) => OnNextCore(value);

    private void OnNextCore(ReadOnlyProperty<ObservableList<TElement>?>? value)
    {
        Debug.Assert(_expression is not null);
        try
        {
            _propertySubscription?.Dispose();

            if (value is { } property)
            {
                _propertySubscription = _expression.Subscribe(
                    property.Changed, this);
            }
            else
            {
                _propertySubscription = null;
                _expression.Next(default);
            }
        }
        catch (Exception error)
        {
            _expression.Error(error);
        }
    }

    public void OnNext(ObservableList<TElement>? value)
    {
        Debug.Assert(_expression is not null);

        _listSubscription?.Dispose();
        _listSubscription = value is not null
            ? _expression.Subscribe(value.Changed, this)
            : null;

        _expression.List = value;
        _expression.Next(value is null ? null : _expression);
    }

    public void OnNext(ListChange<TElement> value)
    {
        Debug.Assert(_expression is not null);
        if (_expression.CollectionChanged is { } changed)
        {
            var args = value.Action switch
            {
                ListChangeAction.Reset => new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Reset),
                ListChangeAction.Insert => new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Add,
                    value.NewItem,
                    value.NewIndex),
                ListChangeAction.Remove => new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Remove,
                    value.OldItem,
                    value.OldIndex),
                ListChangeAction.Replace => new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Replace,
                    value.NewItem,
                    value.OldItem,
                    value.OldIndex),
                ListChangeAction.Move => new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Move,
                    value.NewItem,
                    value.NewIndex,
                    value.OldIndex),
                _ => throw new NotSupportedException()
            };

            changed.Invoke(_expression, args);
        }
    }

    public void OnError(Exception error) => _expression!.Error(error);
    public void OnCompleted() { }
}

internal readonly struct CollectionExpressionFactory<TResult> : IObserverFactory<CollectionExpression<TResult>>
{
    public readonly IAvaloniaObject Target;
    public CollectionExpressionFactory(IAvaloniaObject target) =>
        Target = target;

    public CollectionExpression<TResult> Create<TSource, TStateMachine>(in TStateMachine stateMachine)
        where TStateMachine : struct, IObserverStateMachine<TSource> =>
        new CollectionExpression<TResult, TSource, TStateMachine>(Target, stateMachine);
}