using Kinetic.Runtime;

namespace Kinetic.Linq;

public interface IOperator<T>
{
    public abstract TBox Build<TBox, TBoxFactory, TContinuation>(in TBoxFactory boxFactory, TContinuation continuation)
        where TBoxFactory : struct, IStateMachineBoxFactory<TBox>
        where TContinuation : struct, IStateMachine<T>;
}