using System;
using System.Runtime.CompilerServices;

namespace Kinetic.Linq
{
    public static partial class Observable
    {
        public static ValueTaskAwaiter<TResult> GetAwaiter<TResult>(this IObservable<TResult> source) =>
            source.ToValueTask().GetAwaiter();

        public static ValueTaskAwaiter<TResult> GetAwaiter<TResult>(this in ObserverBuilder<TResult> source) =>
            source.ToValueTask().GetAwaiter();
    }
}