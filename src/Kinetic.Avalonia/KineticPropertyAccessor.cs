using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Avalonia.Data;
using Avalonia.Data.Core.Plugins;

namespace Kinetic.Avalonia
{
    public sealed class KineticPropertyAccessor : IPropertyAccessorPlugin
    {
        private delegate IPropertyAccessor AccessorFactory(WeakReference<object> reference);

        private readonly Dictionary<(Type, string), AccessorFactory?> _propertyLookup = new();
        private readonly MethodInfo _factoryRo = Create(CreateRoAccessorFactory<object>);
        private readonly MethodInfo _factoryRw = Create(CreateRwAccessorFactory<object>);

        private static MethodInfo Create(Func<Func<PropertyInfo, AccessorFactory>> factory) =>
            factory.Method.GetGenericMethodDefinition();

        private static Func<PropertyInfo, AccessorFactory> CreateRoAccessorFactory<T>()
        {
            static AccessorFactory Internal(PropertyInfo property)
            {
                var getter = KineticReflection.CreateRoGetter<T>(property);
                return reference => new AccessorReadOnly<T>(reference, getter);
            }

            return property => Internal(property);
        }

        private static Func<PropertyInfo, AccessorFactory> CreateRwAccessorFactory<T>()
        {
            static AccessorFactory Internal(PropertyInfo property)
            {
                var getter = KineticReflection.CreateRwGetter<T>(property);
                return reference => new Accessor<T>(reference, getter);
            }

            return property => Internal(property);
        }

        public bool Match(object obj, string propertyName)
        {
            return GetAccessorFactory(obj, propertyName) is not null;
        }

        public IPropertyAccessor Start(WeakReference<object> reference, string propertyName)
        {
            reference.TryGetTarget(out var target);

            if (GetAccessorFactory(target, propertyName) is { } factory)
            {
                return factory(reference);
            }
            else
            {
                var message = $"Could not find CLR property '{propertyName}' on '{target}'";
                var exception = new MissingMemberException(message);
                return new PropertyError(new BindingNotification(exception, BindingErrorType.Error));
            }
        }

        private AccessorFactory? GetAccessorFactory(object? obj, string propertyName)
        {
            if (obj is not KineticObject)
            {
                return null;
            }

            var type = obj.GetType();
            var propertyKey = (type, propertyName);

            if (_propertyLookup.TryGetValue(propertyKey, out var factory))
            {
                return factory;
            }

            var property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (property?.PropertyType is { IsConstructedGenericType: true } propertyTypeGeneric)
            {
                var propertyType = propertyTypeGeneric.GetGenericTypeDefinition();
                var method =
                    propertyType == typeof(KineticProperty<>) ? _factoryRw :
                    propertyType == typeof(KineticReadOnlyProperty<>) ? _factoryRo :
                    null;

                factory = method?
                    .MakeGenericMethod(propertyTypeGeneric.GenericTypeArguments)
                    .CreateDelegate<Func<Func<PropertyInfo, AccessorFactory>>>()
                    .Invoke()
                    .Invoke(property);
            }

            _propertyLookup.Add(
                propertyKey,
                factory);

            return factory;
        }

        private sealed class Accessor<T> : AccessorBase<T, KineticPropertyGetter<T>>
        {
            public Accessor(WeakReference<object> reference, KineticPropertyGetter<T> getter)
                : base(reference, getter) { }

            public override object? Value =>
                Getter(Reference, out var property) ? property.Get() : null;

            public override bool SetValue(object value, BindingPriority priority)
            {
                if (Getter(Reference, out var property))
                {
                    property.Set((T) value);
                    return true;
                }

                return false;
            }

            protected override void SubscribeCore() => SubscribeCore(
                Getter(Reference, out var property) ? property.Changed : null);
        }

        private sealed class AccessorReadOnly<T> : AccessorBase<T, KineticReadOnlyPropertyGetter<T>>
        {
            public AccessorReadOnly(WeakReference<object> reference, KineticReadOnlyPropertyGetter<T> getter)
                : base(reference, getter) { }

            public override object? Value =>
                Getter(Reference, out var property) ? property.Get() : null;

            public override bool SetValue(object value, BindingPriority priority) =>
                false;

            protected override void SubscribeCore() => SubscribeCore(
                Getter(Reference, out var property) ? property.Changed : null);
        }

        private abstract class AccessorBase<T, TGetter> : PropertyAccessorBase, IObserver<T>
            where TGetter : Delegate
        {
            protected readonly WeakReference<object> Reference;
            protected readonly TGetter Getter;

            private IDisposable? _subscription;

            protected AccessorBase(WeakReference<object> reference, TGetter getter) =>
                (Reference, Getter) = (reference, getter);

            public override Type PropertyType => typeof(T);

            public void OnCompleted() => throw new NotSupportedException();

            public void OnError(Exception error) => throw new NotSupportedException();

            public void OnNext(T value) => PublishValue(value);

            protected void SubscribeCore(IObservable<T>? observable) =>
                _subscription ??= observable?.Subscribe(this);

            protected override void UnsubscribeCore() =>
                _subscription?.Dispose();
        }

        private static Func<WeakReference<object>, TProperty> CreatePropertyGetter<TOwner, TProperty>(PropertyInfo property)
            where TProperty : struct
        {
            static TProperty Internal(WeakReference<object> reference, Func<TOwner, TProperty> getter) =>
                reference.TryGetTarget(out var target) &&
                target is TOwner owner
                ? getter(owner)
                : default;

            Debug.Assert(property.DeclaringType == typeof(TOwner));
            Debug.Assert(property.PropertyType == typeof(TProperty));
            Debug.Assert(property.GetMethod is not null);

            var getter = property.GetMethod
                .MakeGenericMethod(property.DeclaringType, property.PropertyType)
                .CreateDelegate<Func<TOwner, TProperty>>();

            return reference => Internal(reference, getter);
        }
    }
}