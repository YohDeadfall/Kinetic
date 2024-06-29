namespace Kinetic.Linq.StateMachines;

public interface IStateMachineFactory<T, TResult>
{
    void Create<TContinuation>(in TContinuation continuation, ObserverStateMachine<T> source)
        where TContinuation : struct, IStateMachine<TResult>;
}