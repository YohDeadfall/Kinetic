using System;
using Kinetic.Linq.StateMachines;

namespace Kinetic.Linq
{
    public static partial class Observable
    {
        public static IObservable<T> ToObservable<T>(this in ObserverBuilder<T> source) =>
            source.Build<ObservableStateMachine<T>, ObservableStateMachineBoxFactory<T>, ObservableStateMachineBox<T>>(
                continuation: new(),
                factory: new());
    }
}