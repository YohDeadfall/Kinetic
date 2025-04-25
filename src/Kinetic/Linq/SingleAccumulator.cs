using System;
using System.Diagnostics.CodeAnalysis;
using Kinetic.Runtime;

namespace Kinetic.Linq;

internal struct SingleAccumulator<T> : IAccumulator<T, T>
{
    [AllowNull]
    private T _value;
    private uint _count;

    public SingleAccumulator()
    {
        _value = default;
        _count = 0;
    }

    public SingleAccumulator(T defaultValue)
    {
        _value = defaultValue;
        _count = 1;
    }

    public bool Accumulate(T value)
    {
        if (_count > 2)
            throw new InvalidOperationException("Only one value is expected.");

        _value = value;
        _count = Math.Max(2, _count) + 1;

        return true;
    }

    public void Publish<TStateMachine>(ref TStateMachine stateMachine)
        where TStateMachine : struct, IStateMachine<T>
    {
        if (_count == 0)
            throw new InvalidOperationException("No values were accumulated.");

        stateMachine.OnNext(_value);
    }
}

