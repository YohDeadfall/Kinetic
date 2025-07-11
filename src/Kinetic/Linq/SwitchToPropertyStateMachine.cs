using System;
using Kinetic.Runtime;

namespace Kinetic.Linq;

internal struct SwitchToPropertyStateMachine<TContinuation, TSource> :
    IStateMachine<Property<TSource>?>,
    IStateMachine<ReadOnlyProperty<TSource>?>
    where TContinuation : struct, IStateMachine<TSource>
{
    private SwitchStateMachine<TContinuation, TSource> _continuation;

    public SwitchToPropertyStateMachine(TContinuation continuation) =>
        _continuation = new(continuation);

    public StateMachineBox Box =>
        _continuation.Box;

    StateMachineReference<Property<TSource>?> IStateMachine<Property<TSource>?>.Reference =>
        StateMachineReference<Property<TSource>?>.Create(ref this);

    StateMachineReference<ReadOnlyProperty<TSource>?> IStateMachine<ReadOnlyProperty<TSource>?>.Reference =>
        StateMachineReference<ReadOnlyProperty<TSource>?>.Create(ref this);

    public StateMachineReference? Continuation =>
        _continuation.Continuation;

    public void Dispose() =>
        _continuation.Dispose();

    public void Initialize(StateMachineBox box) =>
        _continuation.Initialize(box);

    public void OnCompleted() =>
        _continuation.OnCompleted();

    public void OnError(Exception error) =>
        _continuation.OnError(error);

    public void OnNext(Property<TSource>? value) =>
        _continuation.OnNext(value?.Changed);

    public void OnNext(ReadOnlyProperty<TSource>? value) =>
        _continuation.OnNext(value?.Changed);
}