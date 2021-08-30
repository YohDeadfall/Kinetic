using System;
using System.Reflection;
using System.Diagnostics;

namespace Kinetic.Avalonia
{
    internal delegate bool KineticPropertyGetter<T>(WeakReference<object> reference, out KineticProperty<T> property);
    internal delegate bool KineticReadOnlyPropertyGetter<T>(WeakReference<object> reference, out KineticReadOnlyProperty<T> property);

    internal static class KineticReflection
    {
        private delegate KineticProperty<T> RwGetter<TOwner, T>(TOwner owner);
        private delegate KineticReadOnlyProperty<T> RoGetter<TOwner, T>(TOwner owner);

        public static MethodInfo GetGenericDefinition<T>(Func<T> method) =>
            method.Method.GetGenericMethodDefinition();

        public static KineticPropertyGetter<T> CreateRwGetter<T>(PropertyInfo property)
        {
            Debug.Assert(property.DeclaringType is not null);
            Debug.Assert(property.PropertyType == typeof(KineticProperty<T>));
            Debug.Assert(property.GetMethod is not null);

            static Func<PropertyInfo, KineticPropertyGetter<T>> Wrapper<TOwner>() =>
                property => Factory<TOwner>(property);

            static KineticPropertyGetter<T> Factory<TOwner>(PropertyInfo property)
            {
                static bool Getter(WeakReference<object> reference, RwGetter<TOwner, T> getter, out KineticProperty<T> property)
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
                Debug.Assert(property.PropertyType == typeof(KineticProperty<T>));
                Debug.Assert(property.GetMethod is not null);

                var getter = property.GetMethod
                    .MakeGenericMethod(property.DeclaringType, property.PropertyType)
                    .CreateDelegate<RwGetter<TOwner, T>>();

                return delegate (WeakReference<object> reference, out KineticProperty<T> property)
                {
                    return Getter(reference, getter, out property);
                };
            }

            return GetGenericDefinition(Wrapper<object>)
                .MakeGenericMethod(property.DeclaringType)
                .CreateDelegate<Func<PropertyInfo, KineticPropertyGetter<T>>>()
                .Invoke(property);
        }

        public static KineticReadOnlyPropertyGetter<T> CreateRoGetter<T>(PropertyInfo property)
        {
            Debug.Assert(property.DeclaringType is not null);
            Debug.Assert(property.PropertyType == typeof(KineticReadOnlyProperty<T>));
            Debug.Assert(property.GetMethod is not null);

            static Func<PropertyInfo, KineticReadOnlyPropertyGetter<T>> Wrapper<TOwner>() =>
                property => Factory<TOwner>(property);

            static KineticReadOnlyPropertyGetter<T> Factory<TOwner>(PropertyInfo property)
            {
                static bool Getter(WeakReference<object> reference, RoGetter<TOwner, T> getter, out KineticReadOnlyProperty<T> property)
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
                Debug.Assert(property.PropertyType == typeof(KineticReadOnlyProperty<T>));
                Debug.Assert(property.GetMethod is not null);

                var getter = property.GetMethod
                    .MakeGenericMethod(property.DeclaringType, property.PropertyType)
                    .CreateDelegate<RoGetter<TOwner, T>>();

                return delegate (WeakReference<object> reference, out KineticReadOnlyProperty<T> property)
                {
                    return Getter(reference, getter, out property);
                };
            }

            return GetGenericDefinition(Wrapper<object>)
                .MakeGenericMethod(property.DeclaringType)
                .CreateDelegate<Func<PropertyInfo, KineticReadOnlyPropertyGetter<T>>>()
                .Invoke(property);
        }

        internal static T CreateDelegate<T>(this MethodInfo method)
            where T : Delegate =>
            (T) method.CreateDelegate(typeof(T));
    }
}