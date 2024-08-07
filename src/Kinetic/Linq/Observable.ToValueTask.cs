using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using Kinetic.Linq.StateMachines;

namespace Kinetic.Linq;

public static partial class Observable
{
    public static ValueTask<T> ToValueTask<T>(this IObservable<T> source) =>
        source.ToBuilder().ToValueTask();

    public static ValueTask<T> ToValueTask<T>(this ObserverBuilder<T> source)
    {
        var taskSource = source.First().Build<ValueTaskSource<T>.ValueTaskStateMachine, ValueTaskSource<T>.BoxFactory, ValueTaskSource<T>.IBoxExternal>(
            continuation: new(),
            factory: new());

        return new ValueTask<T>(taskSource, taskSource.Token);
    }
}

internal static class ValueTaskSource<TResult>
{
    internal interface IBoxExternal : IValueTaskSource<TResult>
    {
        short Token { get; }
    }

    private interface IBox : IBoxExternal
    {
        ref ManualResetValueTaskSourceCore<TResult> Core { get; }
    }

    private sealed class Box<T, TStateMachine> : StateMachineBox<T, TStateMachine>, IBox
        where TStateMachine : struct, IStateMachine<T>
    {
        private ManualResetValueTaskSourceCore<TResult> _core;

        public ref ManualResetValueTaskSourceCore<TResult> Core => ref _core;

        public short Token => _core.Version;

        public Box(in TStateMachine stateMachine) :
            base(stateMachine) => StateMachine.Initialize(this);

        public ValueTaskSourceStatus GetStatus(short token) => _core.GetStatus(token);
        public TResult GetResult(short token) => _core.GetResult(token);

        public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags) =>
            _core.OnCompleted(continuation, state, token, flags);
    }

    internal readonly struct BoxFactory : IStateMachineBoxFactory<IBoxExternal>
    {
        public IBoxExternal Create<T, TStateMachine>(in TStateMachine stateMachine)
            where TStateMachine : struct, IStateMachine<T> =>
            new Box<T, TStateMachine>(stateMachine);
    }

    internal struct ValueTaskStateMachine : IStateMachine<TResult>
    {
        private StateMachineBox? _box;
        private IntPtr _core;

        public StateMachineBox Box =>
            _box ?? throw new InvalidOperationException();

        public StateMachine<TResult> Reference =>
            StateMachine<TResult>.Create(ref this);

        public StateMachine? Continuation =>
            null;

        public void Dispose() { }

        public void Initialize(StateMachineBox box)
        {
            var boxTyped = (IBox) box;

            _box = box;
            _core = Unsafe.ByteOffset(
                ref Unsafe.As<ValueTaskStateMachine, IntPtr>(ref this),
                ref Unsafe.As<ManualResetValueTaskSourceCore<TResult>, IntPtr>(ref boxTyped.Core));
        }

        public void OnCompleted() { }
        public void OnError(Exception error) => GetCore(ref this).SetException(error);
        public void OnNext(TResult value) => GetCore(ref this).SetResult(value);

        private static ref ManualResetValueTaskSourceCore<TResult> GetCore(ref ValueTaskStateMachine self) =>
            ref Unsafe.As<IntPtr, ManualResetValueTaskSourceCore<TResult>>(
                ref Unsafe.AddByteOffset(
                    ref Unsafe.As<ValueTaskStateMachine, IntPtr>(ref self),
                    self._core));
    }
}