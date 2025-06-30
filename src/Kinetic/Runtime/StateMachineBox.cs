using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Kinetic.Runtime;

public abstract class StateMachineBox
{
    private protected abstract ReadOnlySpan<byte> StateMachineData { get; }

    private protected StateMachineBox() { }

    internal IntPtr OffsetTo<T, TStateMachine>(ref TStateMachine stateMachine)
        where TStateMachine : struct, IStateMachine<T>
    {
        var machineHost = StateMachineData;
        var machinePart = MemoryMarshal.CreateSpan(
            ref Unsafe.As<TStateMachine, byte>(ref stateMachine),
            length: Unsafe.SizeOf<TStateMachine>());

        var offset = (nint) Unsafe.ByteOffset(
            ref MemoryMarshal.GetReference(machineHost),
            ref MemoryMarshal.GetReference(machinePart));

        if (offset < 0 && offset + machinePart.Length > machineHost.Length)
            throw new ArgumentException("The provided state machine doesn't belong to the current box.", nameof(stateMachine));

        return offset;
    }

    internal ref TStateMachine ReferenceTo<T, TStateMachine>(IntPtr offset)
    {
        ref var machineHost = ref MemoryMarshal.GetReference(StateMachineData);
        ref var machinePart = ref Unsafe.AddByteOffset(ref machineHost, offset);

        return ref Unsafe.As<byte, TStateMachine>(ref machinePart);
    }
}

[DebuggerTypeProxy(typeof(StateMachineBoxDebugView<,>))]
public abstract class StateMachineBox<T, TStateMachine> : StateMachineBox, IObserver<T>
    where TStateMachine : struct, IEntryStateMachine<T>
{
    private TStateMachine _stateMachine;

    private protected sealed override ReadOnlySpan<byte> StateMachineData =>
        MemoryMarshal.CreateSpan(
            ref Unsafe.As<TStateMachine, byte>(ref _stateMachine),
            length: Unsafe.SizeOf<TStateMachine>());

    protected internal ref TStateMachine StateMachine =>
        ref _stateMachine;

    protected StateMachineBox(in TStateMachine stateMachine) =>
        _stateMachine = stateMachine;

    public void OnCompleted()
    {
        try
        {
            _stateMachine.OnCompleted();
        }
        finally
        {
            _stateMachine.Dispose();
        }
    }

    public void OnError(Exception error)
    {
        try
        {
            _stateMachine.OnError(error);
        }
        finally
        {
            _stateMachine.Dispose();
        }
    }

    public void OnNext(T value)
    {
        try
        {
            _stateMachine.OnNext(value);
        }
        catch
        {
            _stateMachine.Dispose();

            throw;
        }
    }
}