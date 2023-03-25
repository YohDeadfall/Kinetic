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

public readonly struct ObserverStateMachineReference<T, TStateMachine>
    where TStateMachine : struct, IObserverStateMachine<T>
{
    private readonly ObserverStateMachineBox _box;
    private readonly IntPtr _stateMachineOffset;

    public ObserverStateMachineReference(ObserverStateMachineBox box, in TStateMachine stateMachine) =>
        _stateMachineOffset = (_box = box).OffsetTo<T, TStateMachine>(stateMachine);

    public ref TStateMachine Target =>
        ref _box.ReferenceTo<T, TStateMachine>(_stateMachineOffset);
}

public abstract class ObserverStateMachineBox
{
    private protected abstract ReadOnlySpan<byte> StateMachineData { get; }

    private protected ObserverStateMachineBox() { }

    internal IntPtr OffsetTo<T, TStateMachine>(in TStateMachine stateMachine)
        where TStateMachine : struct, IObserverStateMachine<T>
    {
        var machineHost = StateMachineData;
        var machinePart = MemoryMarshal.CreateSpan(
            ref Unsafe.As<TStateMachine, byte>(ref Unsafe.AsRef(stateMachine)),
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

    public IDisposable Subscribe<T, TStateMachine>(IObservable<T> observable, in TStateMachine stateMachine)
        where TStateMachine : struct, IObserverStateMachine<T>
    {
        return Subscribe(observable.ToBuilder(), stateMachine);
    }

    public IDisposable Subscribe<T, TStateMachine>(ObserverBuilder<T> builder, in TStateMachine stateMachine)
        where TStateMachine : struct, IObserverStateMachine<T>
    {
        return builder.Build<SubscribeStateMachine<T, TStateMachine>, SubscribeBoxFactory, IDisposable>(
            new(new ObserverStateMachineReference<T, TStateMachine>(this, stateMachine)), new());
    }

    private readonly struct SubscribeStateMachine<T, TStateMachine> : IObserverStateMachine<T>
        where TStateMachine : struct, IObserverStateMachine<T>
    {
        private readonly ObserverStateMachineReference<T, TStateMachine> _stateMachine;

        public SubscribeStateMachine(ObserverStateMachineReference<T, TStateMachine> stateMachine) =>
            _stateMachine = stateMachine;

        public void Dispose() { }
        public void Initialize(ObserverStateMachineBox box) { }
        public void OnCompleted() => _stateMachine.Target.OnCompleted();
        public void OnError(Exception error) => _stateMachine.Target.OnError(error);
        public void OnNext(T value) => _stateMachine.Target.OnNext(value);
    }

    private readonly struct SubscribeBoxFactory : IObserverFactory<IDisposable>
    {
        public IDisposable Create<T, TStateMachine>(in TStateMachine stateMachine)
            where TStateMachine : struct, IObserverStateMachine<T> =>
            new SubscribeBox<T, TStateMachine>(stateMachine);
    }

    private sealed class SubscribeBox<T, TStateMachine> : ObserverStateMachineBox<T, TStateMachine>, IDisposable
        where TStateMachine : struct, IObserverStateMachine<T>
    {
        public SubscribeBox(in TStateMachine stateMachine) :
            base(stateMachine) => StateMachine.Initialize(this);

        public void Dispose() => StateMachine.Dispose();
    }

    public ObserverBuilder<T> Observe<T, TStateMachine>(IObservable<T> observable, in TStateMachine stateMachine)
        where TStateMachine : struct, IObserverStateMachine<T>
    {
        return Observe(observable.ToBuilder(), stateMachine);
    }

    public ObserverBuilder<T> Observe<T, TStateMachine>(ObserverBuilder<T> builder, in TStateMachine stateMachine)
        where TStateMachine : struct, IObserverStateMachine<T>
    {
        return builder.ContinueWith<ObserveStateMachineFactory<T, TStateMachine>, T>(
            new(new(this, stateMachine)));
    }

    private readonly struct ObserveStateMachineFactory<T, TStateMachine> : IObserverStateMachineFactory<T, T>
        where TStateMachine : struct, IObserverStateMachine<T>
    {
        private readonly ObserverStateMachineReference<T, TStateMachine> _stateMachine;

        public ObserveStateMachineFactory(ObserverStateMachineReference<T, TStateMachine> stateMachine) =>
            _stateMachine = stateMachine;

        public void Create<TContinuation>(in TContinuation continuation, ObserverStateMachine<T> source)
            where TContinuation : struct, IObserverStateMachine<T>
        {
            source.ContinueWith<ObserveStateMachine<T, TStateMachine, TContinuation>>(new(continuation, _stateMachine));
        }
    }

    private struct ObserveStateMachine<T, TStateMachine, TContinuation> : IObserverStateMachine<T>
        where TStateMachine : struct, IObserverStateMachine<T>
        where TContinuation : struct, IObserverStateMachine<T>
    {
        private readonly SubscribeStateMachine<T, TStateMachine> _observer;
        private TContinuation _continuation;

        public ObserveStateMachine(in TContinuation continuation, ObserverStateMachineReference<T, TStateMachine> observer)
        {
            _continuation = continuation;
            _observer = new SubscribeStateMachine<T, TStateMachine>(observer);
        }

        public void Dispose() =>
            _continuation.Dispose();

        public void Initialize(ObserverStateMachineBox box) =>
            _continuation.Initialize(box);

        public void OnCompleted()
        {
            try
            {
                _observer.OnCompleted();
            }
            catch (Exception ex)
            {
                _continuation.OnError(ex);

                return;
            }

            _continuation.OnCompleted();
        }

        public void OnError(Exception error)
        {
            try
            {
                _observer.OnError(error);
            }
            catch (Exception ex)
            {
                _continuation.OnError(ex);

                return;
            }

            _continuation.OnError(error);
        }

        public void OnNext(T value)
        {
            try
            {
                _observer.OnNext(value);
            }
            catch (Exception ex)
            {
                _continuation.OnError(ex);

                return;
            }

            _continuation.OnNext(value);
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