using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Avalonia;
using Avalonia.Data;
using Kinetic.Linq;
using Kinetic.Linq.StateMachines;

namespace Kinetic.Data;

public delegate ObserverBuilder<T> BindingExpressionFactory<T>(ObserverBuilder<object?> source);

public abstract class KineticBinding : IBinding
{
    private const string TwoWayCollectionBindingsNotSupported = "Two way collection bindings are not supported.";

    public abstract InstancedBinding Initiate(
        AvaloniaObject target,
        AvaloniaProperty? targetProperty,
        object? anchor = null,
        bool enableDataValidation = false);

    private protected static InstancedBinding InitiateOneWay<T>(AvaloniaObject target, BindingExpressionFactory<Property<T>?> expression) =>
        InstancedBinding.OneWay(
            expression
                .Invoke(default)
                .ContinueWith<PropertyStateMachineFactory<T>, T?>(default)
                .Build<PublishStateMachine<T?>, BoxFactory<T?>, IBox>(continuation: new(), factory: new(target)));

    private protected static InstancedBinding InitiateOneWay<T>(AvaloniaObject target, BindingExpressionFactory<ReadOnlyProperty<T>?> expression) =>
        InstancedBinding.OneWay(
            expression
                .Invoke(default)
                .ContinueWith<PropertyStateMachineFactory<T>, T?>(default)
                .Build<PublishStateMachine<T?>, BoxFactory<T?>, IBox>(continuation: new(), factory: new(target)));

    private protected static InstancedBinding InitiateOneWay<T>(AvaloniaObject target, BindingExpressionFactory<Property<ObservableList<T>?>?> expression) =>
        InstancedBinding.OneWay(
            expression
                .Invoke(default)
                .ContinueWith<PropertyStateMachineFactory<ObservableList<T>?>, ObservableList<T>?>(default)
                .ContinueWith<ListStateMachineFactory<T>, ListProxy<T>?>(default)
                .Build<PublishStateMachine<ListProxy<T>?>, BoxFactory<ObservableList<T>?>, IBox>(continuation: new(), factory: new(target)));

    private protected static InstancedBinding InitiateOneWay<T>(AvaloniaObject target, BindingExpressionFactory<ReadOnlyProperty<ObservableList<T>?>?> expression) =>
        InstancedBinding.OneWay(
            expression
                .Invoke(default)
                .ContinueWith<PropertyStateMachineFactory<ObservableList<T>?>, ObservableList<T>?>(default)
                .ContinueWith<ListStateMachineFactory<T>, ListProxy<T>?>(default)
                .Build<PublishStateMachine<ListProxy<T>?>, BoxFactory<ObservableList<T>?>, IBox>(continuation: new(), factory: new(target)));

    private protected static InstancedBinding InitiateTwoWay<T>(AvaloniaObject target, BindingExpressionFactory<Property<T>?> expression)
    {
        var stateMachineBox = expression
            .Invoke(default)
            .ContinueWith<PropertyStateMachineFactory<T>, T?>(default)
            .Build<PublishStateMachine<T?>, BoxFactory<T?>, IBox>(continuation: new(), factory: new(target));

        return InstancedBinding.TwoWay(stateMachineBox, stateMachineBox);
    }

    public static KineticBinding OneWay<T>(BindingExpressionFactory<Property<T>?> expression) =>
        new KineticOneWayBinding<T>(expression);

    public static KineticBinding OneWay<T>(BindingExpressionFactory<ReadOnlyProperty<T>?> expression) =>
        new KineticOneWayBinding<T>(expression);

    public static KineticBinding OneWay<T>(BindingExpressionFactory<Property<ObservableList<T>?>?> expression) =>
        new KineticOneWayBinding<T>(expression);

    public static KineticBinding OneWay<T>(BindingExpressionFactory<ReadOnlyProperty<ObservableList<T>?>?> expression) =>
        new KineticOneWayBinding<T>(expression);

    public static KineticBinding TwoWay<T>(BindingExpressionFactory<Property<T>?> expression) =>
        new KineticTwoWayBinding<T>(expression);

    [Obsolete(TwoWayCollectionBindingsNotSupported, error: true)]
    public static KineticBinding TwoWay<T>(BindingExpressionFactory<Property<ObservableList<T>>?> expression) =>
        throw new NotSupportedException(TwoWayCollectionBindingsNotSupported);

    internal interface IBox : IDisposable, IObservable<object?>, IObserver<object?>
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
        private EventHandler<AvaloniaPropertyChangedEventArgs>? _targetChanged;

        private IObserver<object?>? _observer;
        private AvaloniaObject? _target;

        public Property<TProperty>? Property { get; set; }
        public IObserver<object?>? Observer => _observer;

        public Box(in TStateMachine stateMachine, AvaloniaObject target) :
            base(stateMachine)
        {
            _target = target;

            StateMachine.Initialize(this);
        }

        public IDisposable Subscribe(IObserver<object?> observer)
        {
            _targetChanged = _targetChanged is not null
                ? throw new InvalidOperationException()
                : (sender, args) =>
                {
                    if (args.Property == StyledElement.DataContextProperty)
                    {
                        StateMachine.OnNext((TContext) args.NewValue!);
                    }
                };

            _observer = observer;
            _target!.PropertyChanged += _targetChanged;

            try
            {
                StateMachine.Initialize(this);
                StateMachine.OnNext((TContext) _target.GetValue(StyledElement.DataContextProperty)!);
            }
            catch
            {
                StateMachine.Dispose();
                throw;
            }

            return this;
        }

        public void Dispose()
        {
            if (_target is not null)
            {
                _target.PropertyChanged -= _targetChanged;
                _target = null;

                _observer = null;

                StateMachine.Dispose();
            }
        }

        void IObserver<object?>.OnCompleted() { }
        void IObserver<object?>.OnError(Exception error) { }
        void IObserver<object?>.OnNext(object? value) => Property?.Set((TProperty) value!);
    }

    internal readonly struct BoxFactory<TProperty> : IStateMachineBoxFactory<IBox>
    {
        public readonly AvaloniaObject _target;

        public BoxFactory(AvaloniaObject target) =>
            _target = target;

        public IBox Create<T, TStateMachine>(in TStateMachine stateMachine)
            where TStateMachine : struct, IStateMachine<T> =>
            new Box<T, TProperty, TStateMachine>(stateMachine, _target);
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

    internal struct ListStateMachine<TElement, TContinuation> :
        IStateMachine<ObservableList<TElement>?>,
        IStateMachine<ReadOnlyObservableList<TElement>?>
        where TContinuation : struct, IStateMachine<ListProxy<TElement>?>
    {
        private TContinuation _continuation;
        private ListProxy<TElement>? _proxy;

        public ListStateMachine(in TContinuation continuation)
        {
            _continuation = continuation;
            _proxy = null;
        }

        public StateMachineBox Box =>
            _continuation.Box;

        StateMachine<ObservableList<TElement>?> IStateMachine<ObservableList<TElement>?>.Reference =>
            new StateMachine<ObservableList<TElement>?, ListStateMachine<TElement, TContinuation>>(ref this);

        StateMachine<ReadOnlyObservableList<TElement>?> IStateMachine<ReadOnlyObservableList<TElement>?>.Reference =>
            new StateMachine<ReadOnlyObservableList<TElement>?, ListStateMachine<TElement, TContinuation>>(ref this);

        StateMachine? IStateMachine<ObservableList<TElement>?>.Continuation =>
            _continuation.Reference;

        StateMachine? IStateMachine<ReadOnlyObservableList<TElement>?>.Continuation =>
            _continuation.Reference;

        public void Initialize(StateMachineBox box) =>
            _continuation.Initialize(box);

        public void Dispose()
        {
            _continuation.Dispose();
            _proxy?.Dispose();
            _proxy = null;
        }

        public void OnCompleted() =>
            _continuation.OnCompleted();

        public void OnError(Exception error) =>
            _continuation.OnError(error);

        public void OnNext(ObservableList<TElement>? value) =>
            OnNextCore(value);

        public void OnNext(ReadOnlyObservableList<TElement>? value) =>
            OnNextCore(value);

        private void OnNextCore(ReadOnlyObservableList<TElement>? value)
        {
            if (_proxy?.Source != value)
            {
                _proxy?.Dispose();
                _proxy = value is not null
                    ? new ListProxy<TElement>(value)
                    : null;
            }

            _continuation.OnNext(_proxy);
        }
    }

    internal readonly struct ListStateMachineFactory<TElement> :
        IStateMachineFactory<ObservableList<TElement>?, ListProxy<TElement>?>,
        IStateMachineFactory<ReadOnlyObservableList<TElement>?, ListProxy<TElement>?>
    {
        public void Create<TContinuation>(in TContinuation continuation, ObserverStateMachine<ObservableList<TElement>?> source)
            where TContinuation : struct, IStateMachine<ListProxy<TElement>?> =>
            source.ContinueWith(new ListStateMachine<TElement, TContinuation>(continuation));

        public void Create<TContinuation>(in TContinuation continuation, ObserverStateMachine<ReadOnlyObservableList<TElement>?> source)
            where TContinuation : struct, IStateMachine<ListProxy<TElement>?> =>
            source.ContinueWith(new ListStateMachine<TElement, TContinuation>(continuation));
    }

    internal sealed class ListProxy<TElement> : IList, IList<object?>, INotifyCollectionChanged, IObserver<ListChange<TElement>>, IDisposable
    {
        private readonly ReadOnlyObservableList<TElement> _source;
        private readonly IDisposable _sourceChanged;

        private int _count;

        public ListProxy(ReadOnlyObservableList<TElement> source)
        {
            _source = source;
            _sourceChanged = source.Changed.Subscribe(this);
        }

        public ReadOnlyObservableList<TElement> Source => _source;

        public event NotifyCollectionChangedEventHandler? CollectionChanged;

        public object? this[int index]
        {
            get => _source[index];
            set => throw new NotSupportedException();
        }

        public bool IsFixedSize => false;

        public bool IsReadOnly => true;

        public int Count => _count;

        public bool IsSynchronized => false;

        public object SyncRoot => this;

        public void Add(object? value) =>
            throw new NotSupportedException();

        public void Clear() =>
            throw new NotSupportedException();

        public bool Contains(object? value) =>
            IsCompatibleObject(value) && _source.Contains((TElement) value!);

        public void CopyTo(object?[] array, int index)
        {
            for (int offset = _source.Count - 1; offset >= 0; offset -= 1)
            {
                array[index + offset] = _source[index];
            }
        }

        public void CopyTo(Array array, int index)
        {
            for (int offset = _source.Count - 1; offset >= 0; offset -= 1)
            {
                array.SetValue(_source[index], index + offset);
            }
        }

        IEnumerator<object?> IEnumerable<object?>.GetEnumerator()
        {
            var enumerable = _source is IEnumerable<object?> objects
                ? objects : _source.Cast<object?>();

            return enumerable.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() =>
            _source.GetEnumerator();

        public int IndexOf(object? value) =>
            IsCompatibleObject(value) ? _source.IndexOf((TElement) value!) : -1;

        public void Insert(int index, object? value) =>
            throw new NotSupportedException();

        public bool Remove(object? value) =>
            throw new NotSupportedException();
        public void RemoveAt(int index) =>
            throw new NotSupportedException();

        int IList.Add(object? value) => throw new NotSupportedException();
        void IList.Remove(object? value) => throw new NotSupportedException();

        private static bool IsCompatibleObject(object? value) =>
            value is TElement ||
            value is null && default(TElement) is null;

        public void Dispose() =>
            _sourceChanged.Dispose();

        public void OnCompleted() { }

        public void OnError(Exception error) { }

        public void OnNext(ListChange<TElement> value)
        {
            switch (value.Action)
            {
                case ListChangeAction.RemoveAll:
                    _count = 0;
                    break;
                case ListChangeAction.Remove:
                    _count -= 1;
                    break;
                case ListChangeAction.Insert:
                    _count += 1;
                    break;
            }

            CollectionChanged?.Invoke(
                this,
                value.Action switch
                {
                    ListChangeAction.RemoveAll => new NotifyCollectionChangedEventArgs(
                        NotifyCollectionChangedAction.Reset),
                    ListChangeAction.Remove => new NotifyCollectionChangedEventArgs(
                        NotifyCollectionChangedAction.Remove,
                        changedItem: null,
                        value.OldIndex),
                    ListChangeAction.Insert => new NotifyCollectionChangedEventArgs(
                        NotifyCollectionChangedAction.Add,
                        value.NewItem,
                        value.NewIndex),
                    ListChangeAction.Replace => new NotifyCollectionChangedEventArgs(
                        NotifyCollectionChangedAction.Replace,
                        value.NewItem,
                        oldItem: null,
                        value.OldIndex),
                    ListChangeAction.Move => new NotifyCollectionChangedEventArgs(
                        NotifyCollectionChangedAction.Move,
                        _source[value.NewIndex],
                        value.NewIndex,
                        value.OldIndex),
                    _ => throw new NotSupportedException()
                });
        }
    }
}

internal sealed class KineticOneWayBinding<T> : KineticBinding
{
    private readonly Delegate _expression;

    public KineticOneWayBinding(BindingExpressionFactory<Property<T>?> expression) =>
        _expression = expression;

    public KineticOneWayBinding(BindingExpressionFactory<ReadOnlyProperty<T>?> expression) =>
        _expression = expression;

    public KineticOneWayBinding(BindingExpressionFactory<Property<ObservableList<T>?>?> expression) =>
        _expression = expression;

    public KineticOneWayBinding(BindingExpressionFactory<ReadOnlyProperty<ObservableList<T>?>?> expression) =>
        _expression = expression;

    public override InstancedBinding Initiate(
        AvaloniaObject target,
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

internal sealed class KineticTwoWayBinding<T> : KineticBinding
{
    private readonly Delegate _expression;

    public KineticTwoWayBinding(BindingExpressionFactory<Property<T>?> expression) =>
        _expression = expression;

    public KineticTwoWayBinding(BindingExpressionFactory<Property<ObservableList<T>?>?> expression) =>
        _expression = expression;

    public override InstancedBinding Initiate(
        AvaloniaObject target,
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