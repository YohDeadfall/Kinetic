using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Kinetic.Linq.StateMachines;

public interface IStateMachine<T> : IObserver<T>, IDisposable
{
    StateMachineBox Box { get; }

    void Initialize(StateMachineBox box);
}

public interface IStateMachineFactory<T, TResult>
{
    void Create<TContinuation>(in TContinuation continuation, ObserverStateMachine<T> source)
        where TContinuation : struct, IStateMachine<TResult>;
}

public interface IStateMachineBoxFactory<TBox>
{
    TBox Create<T, TStateMachine>(in TStateMachine stateMachine)
        where TStateMachine : struct, IStateMachine<T>;
}

public readonly struct StateMachineReference<T, TStateMachine>
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
}

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

public abstract class StateMachineBox<T, TStateMachine> : StateMachineBox, IObserver<T>
    where TStateMachine : struct, IStateMachine<T>
{
    private TStateMachine _stateMachine;

    private protected sealed override ReadOnlySpan<byte> StateMachineData =>
        MemoryMarshal.CreateSpan(
            ref Unsafe.As<TStateMachine, byte>(ref _stateMachine),
            length: Unsafe.SizeOf<TStateMachine>());

    protected ref TStateMachine StateMachine => ref _stateMachine;

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