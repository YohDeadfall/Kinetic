using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Kinetic.Linq.StateMachines;

public interface IObserverStateMachine<T> : IObserver<T>, IDisposable
{
    void Initialize(ObserverStateMachineBox box);
}

public interface IObserverStateMachineFactory<T, TResult>
{
    void Create<TContinuation>(in TContinuation continuation, ObserverStateMachine<T> source)
        where TContinuation : struct, IObserverStateMachine<TResult>;
}

public interface IObserverFactory<TObserver>
{
    TObserver Create<T, TStateMachine>(in TStateMachine stateMachine)
        where TStateMachine : struct, IObserverStateMachine<T>;
}

public abstract class ObserverStateMachineBox
{
    private protected abstract ReadOnlySpan<byte> StateMachineData { get; }

    private protected ObserverStateMachineBox() { }

    public IDisposable Subscribe<T, TStateMachine>(IObservable<T> observable, in TStateMachine stateMachine)
        where TStateMachine : struct, IObserverStateMachine<T>
    {
        var machineHost = StateMachineData;
        var machinePart = MemoryMarshal.CreateSpan(
            ref Unsafe.As<TStateMachine, byte>(ref Unsafe.AsRef(stateMachine)),
            length: Unsafe.SizeOf<TStateMachine>());

        var offset = (nint) Unsafe.ByteOffset(
            ref MemoryMarshal.GetReference(machineHost),
            ref MemoryMarshal.GetReference(machinePart));

        return offset >= 0 && offset + machinePart.Length <= machineHost.Length
            ? observable.Subscribe(new Observer<T, TStateMachine>(this, offset))
            : throw new ArgumentException("The provided state machine doesn't belong to the current box.", nameof(stateMachine));
    }

    private sealed class Observer<T, TStateMachine> : IObserver<T>
        where TStateMachine : struct, IObserverStateMachine<T>
    {
        private readonly ObserverStateMachineBox _box;
        private readonly IntPtr _stateMachineOffset;

        public Observer(ObserverStateMachineBox box, IntPtr stateMachineOffset)
        {
            _box = box;
            _stateMachineOffset = stateMachineOffset;
        }

        public void OnCompleted() => GetStateMachine().OnCompleted();
        public void OnError(Exception error) => GetStateMachine().OnError(error);
        public void OnNext(T value) => GetStateMachine().OnNext(value);

        private ref TStateMachine GetStateMachine()
        {
            ref var machineHost = ref MemoryMarshal.GetReference(_box.StateMachineData);
            ref var machinePart = ref Unsafe.AddByteOffset(ref machineHost, _stateMachineOffset);

            return ref Unsafe.As<byte, TStateMachine>(ref machinePart);
        }
    }
}

public abstract class ObserverStateMachineBox<T, TStateMachine> : ObserverStateMachineBox, IObserver<T>
    where TStateMachine : struct, IObserverStateMachine<T>
{
    private TStateMachine _stateMachine;

    private protected sealed override ReadOnlySpan<byte> StateMachineData =>
        MemoryMarshal.CreateSpan(
            ref Unsafe.As<TStateMachine, byte>(ref _stateMachine),
            length: Unsafe.SizeOf<TStateMachine>());

    protected ref TStateMachine StateMachine => ref _stateMachine;

    protected ObserverStateMachineBox(in TStateMachine stateMachine) =>
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