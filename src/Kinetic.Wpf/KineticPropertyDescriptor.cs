using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Kinetic.Data;

internal abstract class KineticPropertyDescriptor : PropertyDescriptor
{
    public static readonly Type MutablePropertyType = typeof(Property<>);
    public static readonly Type ImmutablePropertyType = typeof(ReadOnlyProperty<>);

    protected KineticPropertyDescriptor(PropertyDescriptor parent) :
        base(parent) => ComponentType = parent.ComponentType;

    public static KineticPropertyDescriptor? TryCreate(PropertyDescriptor property)
    {
        static KineticPropertyDescriptor Factory<T>(PropertyDescriptor property) =>
            new KineticPropertyDescriptor<T>(property);

        return
            property.PropertyType is { IsGenericType: true } type &&
            type.GetGenericTypeDefinition() is var genericType && (
                genericType == MutablePropertyType ||
                genericType == ImmutablePropertyType) &&
            type.GetGenericArguments() is [var actualType]
            ? Reflection
                .GetGenericDefinition(Factory<object>)
                .MakeGenericMethod(actualType)
                .CreateDelegate<Func<PropertyDescriptor, KineticPropertyDescriptor>>()
                .Invoke(property)
            : null;
    }

    public sealed override Type ComponentType { get; }

    public sealed override bool SupportsChangeEvents => true;

    public sealed override bool CanResetValue(object component) => false;

    public sealed override void ResetValue(object component) =>
        throw new NotSupportedException();
}

internal sealed class KineticPropertyDescriptor<T> : KineticPropertyDescriptor
{
    private readonly Delegate _accessor;
    private Dictionary<object, ValueChangedObserver>? _observers;

    internal KineticPropertyDescriptor(PropertyDescriptor parent) :
        base(parent)
    {
        var property = ComponentType.GetProperty(Name);
        var isReadOnly = property?.PropertyType == typeof(ReadOnlyProperty<T>);

        _accessor = property?.GetMethod is { } getter
            ? isReadOnly
                ? (Delegate) Reflection.CreateRoGetter<T>(getter)
                : (Delegate) Reflection.CreateRwGetter<T>(getter)
            : isReadOnly
                ? (Delegate) ((object? component) => (ReadOnlyProperty<T>) parent.GetValue(component))
                : (Delegate) ((object? component) => (Property<T>) parent.GetValue(component));
    }

    public override Type PropertyType => typeof(T);

    public override bool IsReadOnly => _accessor is Func<object?, ReadOnlyProperty<T>>;

    public override void AddValueChanged(object component, EventHandler value)
    {
        ArgumentNullException.ThrowIfNull(component, nameof(component));
        ArgumentNullException.ThrowIfNull(value, nameof(value));

        var source = (ObservableObject) component;
        var handler = (EventHandler) value;

        _observers ??= new Dictionary<object, ValueChangedObserver>();
        _observers.TryGetValue(component, out var observer);

        if (observer is null)
        {
            observer = new ValueChangedObserver(source);
            observer.Subscription = GetPropertyAsImmutable(component)
                .Changed
                .Subscribe(observer);

            _observers.Add(component, observer);
        }

        observer.Handlers += handler;
    }

    public override void RemoveValueChanged(object component, EventHandler value)
    {
        ArgumentNullException.ThrowIfNull(component, nameof(component));
        ArgumentNullException.ThrowIfNull(value, nameof(value));

        if (_observers is { } &&
            _observers.TryGetValue(component, out var observer))
        {
            observer.Handlers -= (EventHandler) value;

            if (observer.Handlers is null)
            {
                observer.Subscription?.Dispose();

                _observers.Remove(component);
            }
        }
    }

    public override object? GetValue(object? component)
    {
        ArgumentNullException.ThrowIfNull(component, nameof(component));

        return GetPropertyAsImmutable(component).Get();
    }

    public override void SetValue(object? component, object? value)
    {
        ArgumentNullException.ThrowIfNull(component, nameof(component));

        GetPropertyAsMutable(component).Set((T) value!);
    }

    public override bool ShouldSerializeValue(object component) =>
        !IsReadOnly;

    private Property<T> GetPropertyAsMutable(object component) =>
        _accessor switch
        {
            Func<object, Property<T>> accessor => accessor(component),
            _ => throw new NotSupportedException()
        };

    private ReadOnlyProperty<T> GetPropertyAsImmutable(object component) =>
        _accessor switch
        {
            Func<object, Property<T>> accessor => accessor(component),
            Func<object, ReadOnlyProperty<T>> accessor => accessor(component),
            _ => throw new NotSupportedException()
        };

    private sealed class ValueChangedObserver : IObserver<T>
    {
        public ObservableObject Source { get; }
        public EventHandler? Handlers { get; set; }
        public IDisposable? Subscription { get; set; }

        public ValueChangedObserver(ObservableObject source) =>
            Source = source;

        public void OnNext(T value) =>
            Handlers?.Invoke(Source, EventArgs.Empty);

        public void OnError(Exception error) { }

        public void OnCompleted() { }
    }
}