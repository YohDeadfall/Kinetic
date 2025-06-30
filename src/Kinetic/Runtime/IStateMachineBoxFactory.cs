namespace Kinetic.Runtime;

public interface IStateMachineBoxFactory<TBox>
{
    TBox Create<TSource, TStateMachine>(TStateMachine stateMachine)
        where TStateMachine : struct, IEntryStateMachine<TSource>;
}