namespace Kinetic.Runtime;

public interface IEntryStateMachine<T> : IStateMachine<T>
{
    void Start();
}