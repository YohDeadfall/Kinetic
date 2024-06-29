namespace Kinetic.Linq.StateMachines;

public interface IStateMachineBoxFactory<TBox>
{
    TBox Create<T, TStateMachine>(in TStateMachine stateMachine)
        where TStateMachine : struct, IStateMachine<T>;
}