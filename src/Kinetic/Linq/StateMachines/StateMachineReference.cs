using System;
using System.Diagnostics.CodeAnalysis;

namespace Kinetic.Linq.StateMachines;

public readonly struct StateMachineReference<T, TStateMachine> : IEquatable<StateMachineReference<T, TStateMachine>>
    where TStateMachine : struct, IStateMachine<T>
{
    private readonly StateMachineBox _box;
    private readonly IntPtr _stateMachineOffset;

    public StateMachineReference(ref TStateMachine stateMachine)
    {
        _box = stateMachine.Box;
        _stateMachineOffset = _box.OffsetTo<T, TStateMachine>(ref stateMachine);
    }

    public ref TStateMachine Target =>
        ref _box.ReferenceTo<T, TStateMachine>(_stateMachineOffset);

    public bool Equals(StateMachineReference<T, TStateMachine> other) =>
        other._box == _box && other._stateMachineOffset == _stateMachineOffset;

    public override bool Equals([NotNullWhen(true)] object? obj) =>
        obj is StateMachineReference<T, TStateMachine> other && other.Equals(this);

    public override int GetHashCode() =>
        HashCode.Combine(_box, _stateMachineOffset);
}