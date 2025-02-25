using System;
using System.Diagnostics;
using System.Reflection;

namespace Kinetic.Data;

internal static class Reflection
{
    private delegate Property<T> RwGetter<TOwner, T>(TOwner owner);
    private delegate ReadOnlyProperty<T> RoGetter<TOwner, T>(TOwner owner);

    public static MethodInfo GetGenericDefinition(Delegate method) =>
        method.Method.GetGenericMethodDefinition();

    public static Func<object, Property<T>> CreateRwGetter<T>(MethodInfo method)
    {
        static Func<MethodInfo, Func<object?, Property<T>>> Wrapper<TOwner>() =>
            method => Factory<TOwner>(method);

        static Func<object?, Property<T>> Factory<TOwner>(MethodInfo method)
        {
            var getter = method.CreateDelegate<RwGetter<TOwner?, T>>();
            return reference => getter((TOwner?) reference);
        }

        Debug.Assert(method.DeclaringType != null);

        return GetGenericDefinition(Wrapper<object>)
            .MakeGenericMethod(typeof(T), method.DeclaringType)
            .CreateDelegate<Func<Func<MethodInfo, Func<object?, Property<T>>>>>()
            .Invoke()
            .Invoke(method);
    }

    public static Func<object?, ReadOnlyProperty<T>> CreateRoGetter<T>(MethodInfo method)
    {
        static Func<MethodInfo, Func<object, ReadOnlyProperty<T>>> Wrapper<TOwner>() =>
            method => Factory<TOwner>(method);

        static Func<object?, ReadOnlyProperty<T>> Factory<TOwner>(MethodInfo method)
        {
            var getter = method.CreateDelegate<RoGetter<TOwner?, T>>();
            return reference => getter((TOwner?) reference);
        }

        Debug.Assert(method.DeclaringType != null);

        try
        {
            return GetGenericDefinition(Wrapper<object>)
                .MakeGenericMethod(typeof(T), method.DeclaringType)
                .CreateDelegate<Func<Func<MethodInfo, Func<object?, ReadOnlyProperty<T>>>>>()
                .Invoke()
                .Invoke(method);
        }
        catch (Exception ex)
        {
            throw new Exception(ex.ToString() + "\n" + method.ToString());
        }
    }

    public static T CreateDelegate<T>(this MethodInfo method)
        where T : Delegate =>
        (T) method.CreateDelegate(typeof(T));
}