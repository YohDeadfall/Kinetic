using System;

namespace Kinetic.Linq.StateMachines;

public abstract class StateMachine
{
    private protected StateMachine() { }
}

public abstract class StateMachine<T> : StateMachine, IObserver<T>
{
    private protected StateMachine() { }

    public abstract void OnCompleted();
    public abstract void OnError(Exception error);
    public abstract void OnNext(T value);

    internal static StateMachine<T> Create<TStateMachine>(ref TStateMachine stateMachine)
        where TStateMachine : struct, IStateMachine<T> =>
        new StateMachine<T, TStateMachine>(ref stateMachine);
}

public class StateMachine<T, TStateMachine> : StateMachine<T>
    where TStateMachine : struct, IStateMachine<T>
{
    protected StateMachineReference<T, TStateMachine> Reference { get; }

    public StateMachine(ref TStateMachine stateMachine) :
        this(new StateMachineReference<T, TStateMachine>(ref stateMachine))
    { }

    public StateMachine(StateMachineReference<T, TStateMachine> stateMchine) =>
        Reference = stateMchine;

    public override void OnCompleted() =>
        Reference.Target.OnCompleted();

    public override void OnError(Exception error) =>
        Reference.Target.OnError(error);

    public override void OnNext(T value) =>
        Reference.Target.OnNext(value);
}