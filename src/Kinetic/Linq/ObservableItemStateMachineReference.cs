using System;
using Kinetic.Runtime;

namespace Kinetic.Linq;

internal sealed class ObservableItemStateMachineReference<T, TSource, TStateMachine> :
    StateMachineReference<TSource, TStateMachine>,
    IObservableItemStateMachine<T>
    where TStateMachine : struct, IStateMachine<TSource>, IObservableItemStateMachine<T>
{
    public ObservableItemStateMachineReference(ref TStateMachine stateMachine) :
        base(ref stateMachine)
    { }

    public IObservableItemStateMachine<T> Reference =>
        this;

    public void OnItemCompleted(ObservableViewItem<T> item, Exception? error) =>
        Target.OnItemCompleted(item, error);

    public void OnItemUpdated(ObservableViewItem<T> item) =>
        Target.OnItemUpdated(item);
}