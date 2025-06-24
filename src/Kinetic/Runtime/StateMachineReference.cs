using System;
using System.Diagnostics;

namespace Kinetic.Runtime;

public abstract class StateMachineReference
{
    private protected StateMachineReference() { }

    internal abstract StateMachineReference? Continuation { get; }
}

public abstract class StateMachineReference<T> : StateMachineReference, IObserver<T>
{
    private protected StateMachineReference() { }

    public abstract void OnCompleted();
    public abstract void OnError(Exception error);
    public abstract void OnNext(T value);

    public static StateMachineReference<T> Create<TStateMachine>(ref TStateMachine stateMachine)
        where TStateMachine : struct, IStateMachine<T> =>
        new StateMachineReference<T, TStateMachine>(ref stateMachine);
}

[DebuggerTypeProxy(typeof(StateMachineReferenceDebugView<,>))]
public class StateMachineReference<T, TStateMachine> : StateMachineReference<T>
    where TStateMachine : struct, IStateMachine<T>
{
    private readonly StateMachineValueReference<T, TStateMachine> _reference;

    public StateMachineReference(ref TStateMachine stateMachine) :
        this(new StateMachineValueReference<T, TStateMachine>(ref stateMachine))
    { }

    public StateMachineReference(StateMachineValueReference<T, TStateMachine> stateMachine) =>
        _reference = stateMachine;

    public ref TStateMachine Target =>
        ref _reference.Target;

    internal override StateMachineReference? Continuation =>
        _reference.Target.Continuation;

    public override void OnCompleted() =>
        _reference.Target.OnCompleted();

    public override void OnError(Exception error) =>
        _reference.Target.OnError(error);

    public override void OnNext(T value) =>
        _reference.Target.OnNext(value);

    public static implicit operator StateMachineValueReference<T, TStateMachine>(StateMachineReference<T, TStateMachine> reference) =>
        reference._reference;
}