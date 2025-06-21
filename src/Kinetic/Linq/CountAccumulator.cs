using Kinetic.Runtime;

namespace Kinetic.Linq;

internal struct CountAccumulator<T> : IAccumulator<T, int>
{
    private int _count;

    public bool Accumulate(T value)
    {
        _count = checked(_count + 1);
        return true;
    }

    public void Publish<TStateMachine>(ref TStateMachine stateMachine)
        where TStateMachine : struct, IStateMachine<int>
    {
        stateMachine.OnNext(_count);
    }
}