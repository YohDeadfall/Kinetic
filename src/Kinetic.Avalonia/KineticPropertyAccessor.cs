using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Reflection;
using Avalonia.Data;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;

namespace Kinetic.Data;

public sealed class KineticPropertyAccessor : IPropertyAccessorPlugin
{
    private delegate IPropertyAccessor? AccessorFactory(WeakReference<object?> reference);

    private readonly Dictionary<(Type, string), AccessorFactory?> _propertyLookup = new();
    private readonly MethodInfo _factoryRo = GetGenericMethod(CreateRoAccessorFactory<object>);
    private readonly MethodInfo _factoryRw = GetGenericMethod(CreateRwAccessorFactory<object>);
    private readonly MethodInfo _factoryList = GetGenericMethod(CreateListAccessorExternalFactory<object>);

    private static MethodInfo GetGenericMethod<T>(Func<T> factory) =>
        factory.Method.GetGenericMethodDefinition();

    private static Func<PropertyInfo, AccessorFactory> CreateRoAccessorFactory<T>() =>
        static property =>
        {
            var getter = Reflection.CreateRoGetter<T>(property);
            return reference => new AccessorReadOnly<T>(reference, getter);
        };

    private static Func<PropertyInfo, AccessorFactory> CreateRwAccessorFactory<T>() =>
        static property =>
        {
            var getter = Reflection.CreateRwGetter<T>(property);
            return reference => new Accessor<T>(reference, getter);
        };

    private static Func<AccessorFactory, AccessorFactory> CreateListAccessorExternalFactory<T>() =>
        static factory => reference =>
        {
            return factory(reference) is { } accessor
                ? new ListAccessor<T>(accessor) : null;
        };

    public bool Match(object obj, string propertyName)
    {
        return obj is ListProxy || GetAccessorFactory(obj, propertyName) is not null;
    }

    public IPropertyAccessor? Start(WeakReference<object?> reference, string propertyName)
    {
        if (reference.TryGetTarget(out var obj) && obj is not null)
        {
            if (GetAccessorFactory(obj, propertyName) is { } factory)
            {
                return factory(reference);
            }
            else
            {
                var message = $"Could not find CLR property '{propertyName}' on '{obj.GetType()}'";
                var exception = new MissingMemberException(message);
                return new PropertyError(new BindingNotification(exception, BindingErrorType.Error));
            }
        }

        return null;
    }

    private IPropertyAccessorPlugin? GetAccessorPlugin(object obj, string propertyName)
    {
        foreach (var accessor in ExpressionObserver.PropertyAccessors)
        {
            if (accessor is not KineticPropertyAccessor &&
                accessor.Match(obj, propertyName))
            {
                return accessor;
            }
        }

        return null;
    }

    private AccessorFactory? GetAccessorFactory(object? obj, string propertyName)
    {
        if (obj is null)
        {
            return null;
        }

        var type = obj.GetType();
        var propertyKey = (type, propertyName);

        if (_propertyLookup.TryGetValue(propertyKey, out var factory))
        {
            return factory;
        }

        if (obj is ListProxy proxy)
        {
            factory = GetAccessorPlugin(obj, propertyName) is { } plugin
                ? reference => plugin.Start(reference, propertyName)
                : null;
        }
        else
        {
            var property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (property?.PropertyType is { } propertyType)
            {
                if (propertyType.IsConstructedGenericType)
                {
                    var propertyTypeGeneric = propertyType.GetGenericTypeDefinition();
                    var method =
                        propertyTypeGeneric == typeof(Property<>) ? _factoryRw :
                        propertyTypeGeneric == typeof(ReadOnlyProperty<>) ? _factoryRo :
                        null;

                    if (method is not null &&
                        propertyType.GetGenericArguments() is { Length: 1 } argumentTypes)
                    {
                        propertyType = argumentTypes[0];
                        factory = method
                            .MakeGenericMethod(argumentTypes)
                            .CreateDelegate<Func<Func<PropertyInfo, AccessorFactory>>>()
                            .Invoke()
                            .Invoke(property);
                    }
                }

                for (
                    Type?
                        currentType = propertyType,
                        listType = typeof(ReadOnlyObservableList<>);
                    currentType != null;
                    currentType = currentType.BaseType)
                {
                    if (currentType.IsConstructedGenericType &&
                        currentType.GetGenericTypeDefinition() == listType)
                    {
                        factory ??= GetAccessorPlugin(obj, propertyName) is { } plugin
                            ? reference => plugin.Start(reference, propertyName)
                            : null;

                        if (factory != null)
                        {
                            factory = _factoryList
                                .MakeGenericMethod(currentType.GetGenericArguments())
                                .CreateDelegate<Func<Func<AccessorFactory, AccessorFactory>>>()
                                .Invoke()
                                .Invoke(factory);

                            break;
                        }
                    }
                }
            }
        }

        _propertyLookup.Add(
            propertyKey,
            factory);

        return factory;
    }

    private sealed class Accessor<T> : AccessorBase<T, PropertyGetter<T>>
    {
        public Accessor(WeakReference<object?> reference, PropertyGetter<T> getter)
            : base(reference, getter) { }

        public override object? Value =>
            Getter(Reference, out var property) ? property.Get() : null;

        public override bool SetValue(object? value, BindingPriority priority)
        {
            if (Getter(Reference, out var property))
            {
                property.Set((T) value!);
                return true;
            }

            return false;
        }

        protected override void SubscribeCore() =>
            SubscribeCore(Getter(Reference, out var property) ? property.Changed : null);
    }

    private sealed class AccessorReadOnly<T> : AccessorBase<T, ReadOnlyPropertyGetter<T>>
    {
        public AccessorReadOnly(WeakReference<object?> reference, ReadOnlyPropertyGetter<T> getter)
            : base(reference, getter) { }

        public override object? Value =>
            Getter(Reference, out var property) ? property.Get() : null;

        public override bool SetValue(object? value, BindingPriority priority) =>
            false;

        protected override void SubscribeCore() =>
            SubscribeCore(Getter(Reference, out var property) ? property.Changed : null);
    }

    private abstract class AccessorBase<T, TGetter> : PropertyAccessorBase, IObserver<T>
        where TGetter : Delegate
    {
        protected readonly WeakReference<object?> Reference;
        protected readonly TGetter Getter;

        private IDisposable? _subscription;

        protected AccessorBase(WeakReference<object?> reference, TGetter getter) =>
            (Reference, Getter) = (reference, getter);

        public override Type PropertyType => typeof(T);

        public void OnCompleted() { }

        public void OnError(Exception error) { }

        public void OnNext(T value) => PublishValue(value);

        protected void SubscribeCore(IObservable<T>? observable)
        {
            _subscription?.Dispose();
            _subscription = observable?.Subscribe(this);
        }

        protected override void UnsubscribeCore()
        {
            _subscription?.Dispose();
            _subscription = null;
        }
    }

    private abstract class ListProxy
    {
        public abstract IEnumerable Source { get; }

        protected static Exception NotSupported() =>
            new NotSupportedException("The bound list cannot be changed.");
    }

    private sealed class ListProxy<T>
        : ListProxy
        , IObserver<ListChange<T>>
        , INotifyCollectionChanged
        , IList
    {
        private readonly ReadOnlyObservableList<T> _list;
        private readonly IDisposable _subscription;

        private int _count;

        public ListProxy(ReadOnlyObservableList<T> list)
        {
            _list = list;
            _subscription = list.Changed.Subscribe(this);
        }

        public override IEnumerable Source => _list;

        public int Count => _count;

        public object? this[int index]
        {
            get => _list[index];
            set => throw NotSupported();
        }

        bool IList.IsFixedSize => false;

        bool IList.IsReadOnly => true;

        bool ICollection.IsSynchronized => false;

        object ICollection.SyncRoot => this;

        public event NotifyCollectionChangedEventHandler? CollectionChanged;

        public int Add(object? value) =>
            throw NotSupported();

        public void Clear() =>
            throw NotSupported();

        public bool Contains(object? value) =>
            IsCompatibleObject(value) && _list.Contains((T) value!);

        public void CopyTo(Array array, int index)
        {
            var list = _list;
            if (array is T[] arrayTyped)
            {
                list.CopyTo(arrayTyped, index);
            }
            else
            {
                for (var current = _count - 1; current >= 0; current -= 1)
                {
                    array.SetValue(list[current], index + current);
                }
            }
        }

        public IEnumerator GetEnumerator() =>
            _list.GetEnumerator();

        public int IndexOf(object? value) =>
            IsCompatibleObject(value) ? _list.IndexOf((T) value!) : -1;

        public void Insert(int index, object? value) =>
            throw NotSupported();

        public void Remove(object? value) =>
            throw NotSupported();
        public void RemoveAt(int index) =>
            throw NotSupported();

        public void Dispose() =>
            _subscription.Dispose();

        public void OnCompleted() { }
        public void OnError(Exception error) { }
        public void OnNext(ListChange<T> value)
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
                sender: this,
                e: value.Action switch
                {
                    ListChangeAction.RemoveAll => new NotifyCollectionChangedEventArgs(
                        NotifyCollectionChangedAction.Reset),
                    ListChangeAction.Remove => new NotifyCollectionChangedEventArgs(
                        NotifyCollectionChangedAction.Remove,
                        value.OldItem,
                        value.OldIndex),
                    ListChangeAction.Insert => new NotifyCollectionChangedEventArgs(
                        NotifyCollectionChangedAction.Add,
                        value.NewItem,
                        value.NewIndex),
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
                });
        }

        private static bool IsCompatibleObject(object? value) =>
            value is T || value is null && default(T) is null;
    }

    private sealed class ListAccessor<T> : IPropertyAccessor
    {
        private readonly IPropertyAccessor _accessor;

        private Action<object?>? _listener;
        private ListProxy<T>? _list;

        public ListAccessor(IPropertyAccessor accessor) =>
            _accessor = accessor;

        public Type? PropertyType => _list?.GetType();

        public object? Value => _list;

        public bool SetValue(object? value, BindingPriority priority) => false;

        public void Subscribe(Action<object?> listener)
        {
            if (_listener != null)
            {
                throw new InvalidOperationException(
                    "A member accessor can be subscribed to only once.");
            }

            _listener = listener;
            _accessor.Subscribe(OnValueChanged);
        }

        public void Unsubscribe()
        {
            if (_listener == null)
            {
                throw new InvalidOperationException(
                     "The member accessor was not subscribed.");
            }

            _accessor.Unsubscribe();
            _listener = null;
        }

        public void Dispose()
        {
            _listener = null;
            _list = null;

            _accessor.Dispose();
        }

        private void OnValueChanged(object? value)
        {
            _list?.Dispose();
            _list = value is ReadOnlyObservableList<T> list
                ? new ListProxy<T>(list)
                : null;

            _listener?.Invoke(_list);
        }
    }
}