using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Kinetic;

internal static class ObjectExtensions
{
    public static T ThrowIfArgumentNull<T>(this T obj, [CallerArgumentExpression("obj")] string? paramName = null)
    {
        if (obj is null)
            throw new ArgumentNullException(paramName);
        return obj;
    }

    public static T ThrowIfArgumentNegative<T>(this T obj, [CallerArgumentExpression("obj")] string? paramName = null)
        where T : INumber<T>
    {
        if (T.Sign(obj) == -1)
            throw new ArgumentOutOfRangeException(paramName, "The value cannot be negative.");
        return obj;
    }
}