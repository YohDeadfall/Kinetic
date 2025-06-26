using System;

namespace Kinetic.Linq;

internal interface IObservableItemStateMachine<T>
{
    IObservableItemStateMachine<T> Reference { get; }

    void OnItemCompleted(ObservableViewItem<T> item, Exception? error);
    void OnItemUpdated(ObservableViewItem<T> item);
}