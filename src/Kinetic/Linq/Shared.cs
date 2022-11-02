using System;
using System.Runtime.ExceptionServices;

namespace Kinetic.Linq;

public static partial class Observable
{
    private static readonly Action<Exception> ThrowOnError = (error) => ExceptionDispatchInfo.Throw(error);
    private static readonly Action<Exception> NothingOnError = (error) => { };
    private static readonly Action NothingOnCompleted = () => { };
}