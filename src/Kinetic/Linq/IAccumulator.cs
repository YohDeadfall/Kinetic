using Kinetic.Runtime;

namespace Kinetic.Linq;

internal interface IAccumulator<T, TResult>
{
    bool Accumulate(T value);
    void Publish<TStateMachine>(ref TStateMachine stateMachine)
        where TStateMachine : struct, IStateMachine<TResult>;
}