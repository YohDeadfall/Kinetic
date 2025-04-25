using System;

namespace Kinetic;

internal interface IObservableInternal<T> : IObservable<T>
{
    void Subscribe(ObservableSubscription<T> subscription);
    void Unsubscribe(ObservableSubscription<T> subscription);
}
