using System;
using System.Diagnostics;
using System.Reflection;

namespace Kinetic.Avalonia
{
    internal delegate bool PropertyGetter<T>(WeakReference<object> reference, out Property<T> property);
    internal delegate bool ReadOnlyPropertyGetter<T>(WeakReference<object> reference, out ReadOnlyProperty<T> property);

    internal static class Reflection
    {
        private delegate Property<T> RwGetter<TOwner, T>(TOwner owner);
        private delegate ReadOnlyProperty<T> RoGetter<TOwner, T>(TOwner owner);

        public static MethodInfo GetGenericDefinition<T>(Func<T> method) =>
            method.Method.GetGenericMethodDefinition();

        public static PropertyGetter<T> CreateRwGetter<T>(PropertyInfo property)
        {
            Debug.Assert(property.DeclaringType is not null);
            Debug.Assert(property.PropertyType == typeof(Property<T>));
            Debug.Assert(property.GetMethod is not null);

            static Func<PropertyInfo, PropertyGetter<T>> Wrapper<TOwner>() =>
                property => Factory<TOwner>(property);

            static PropertyGetter<T> Factory<TOwner>(PropertyInfo property)
            {
                static bool Getter(WeakReference<object> reference, RwGetter<TOwner, T> getter, out Property<T> property)
                {
                    if (reference.TryGetTarget(out var target) &&
                        target is TOwner owner)
                    {
                        property = getter(owner);
                        return true;
                    }
                    else
                    {
                        property = default;
                        return false;
                    }
                }

                Debug.Assert(property.DeclaringType == typeof(TOwner));
                Debug.Assert(property.PropertyType == typeof(Property<T>));
                Debug.Assert(property.GetMethod is not null);

                var getter = property.GetMethod
                    .MakeGenericMethod(property.DeclaringType, property.PropertyType)
                    .CreateDelegate<RwGetter<TOwner, T>>();

                return delegate (WeakReference<object> reference, out Property<T> property)
                {
                    return Getter(reference, getter, out property);
                };
            }

            return GetGenericDefinition(Wrapper<object>)
                .MakeGenericMethod(property.DeclaringType)
                .CreateDelegate<Func<PropertyInfo, PropertyGetter<T>>>()
                .Invoke(property);
        }

        public static ReadOnlyPropertyGetter<T> CreateRoGetter<T>(PropertyInfo property)
        {
            Debug.Assert(property.DeclaringType is not null);
            Debug.Assert(property.PropertyType == typeof(ReadOnlyProperty<T>));
            Debug.Assert(property.GetMethod is not null);

            static Func<PropertyInfo, ReadOnlyPropertyGetter<T>> Wrapper<TOwner>() =>
                property => Factory<TOwner>(property);

            static ReadOnlyPropertyGetter<T> Factory<TOwner>(PropertyInfo property)
            {
                static bool Getter(WeakReference<object> reference, RoGetter<TOwner, T> getter, out ReadOnlyProperty<T> property)
                {
                    if (reference.TryGetTarget(out var target) &&
                        target is TOwner owner)
                    {
                        property = getter(owner);
                        return true;
                    }
                    else
                    {
                        property = default;
                        return false;
                    }
                }

                Debug.Assert(property.DeclaringType == typeof(TOwner));
                Debug.Assert(property.PropertyType == typeof(ReadOnlyProperty<T>));
                Debug.Assert(property.GetMethod is not null);

                var getter = property.GetMethod
                    .MakeGenericMethod(property.DeclaringType, property.PropertyType)
                    .CreateDelegate<RoGetter<TOwner, T>>();

                return delegate (WeakReference<object> reference, out ReadOnlyProperty<T> property)
                {
                    return Getter(reference, getter, out property);
                };
            }

            return GetGenericDefinition(Wrapper<object>)
                .MakeGenericMethod(property.DeclaringType)
                .CreateDelegate<Func<PropertyInfo, ReadOnlyPropertyGetter<T>>>()
                .Invoke(property);
        }

        internal static T CreateDelegate<T>(this MethodInfo method)
            where T : Delegate =>
            (T) method.CreateDelegate(typeof(T));
    }
}