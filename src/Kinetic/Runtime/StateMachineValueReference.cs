using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Kinetic.Runtime;

public static class StateMachineValueReference<T>
{
    public static StateMachineValueReference<T, TStateMachine> Create<TStateMachine>(ref TStateMachine stateMachine)
        where TStateMachine : struct, IStateMachine<T>
    {
        return new(ref stateMachine);
    }
}

[DebuggerTypeProxy(typeof(StateMachineReferenceDebugView<,>))]
public readonly struct StateMachineValueReference<T, TStateMachine> : IEquatable<StateMachineValueReference<T, TStateMachine>>
    where TStateMachine : struct, IStateMachine<T>
{
    private readonly StateMachineBox _box;
    private readonly IntPtr _stateMachineOffset;

    public StateMachineValueReference(ref TStateMachine stateMachine)
    {
        _box = stateMachine.Box;
        _stateMachineOffset = _box.OffsetTo<T, TStateMachine>(ref stateMachine);
    }

    public ref TStateMachine Target =>
        ref _box.ReferenceTo<T, TStateMachine>(_stateMachineOffset);

    public bool Equals(StateMachineValueReference<T, TStateMachine> other) =>
        other._box == _box && other._stateMachineOffset == _stateMachineOffset;

    public override bool Equals([NotNullWhen(true)] object? obj) =>
        obj is StateMachineValueReference<T, TStateMachine> other && other.Equals(this);

    public override int GetHashCode() =>
        HashCode.Combine(_box, _stateMachineOffset);
}
