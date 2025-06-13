using System;
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
        void Initialize(ref ValueTaskStateMachine publisher);
    }

    private sealed class Box<T, TStateMachine> : StateMachineBox<T, TStateMachine>, IBox
        where TStateMachine : struct, IStateMachine<T>
    {
        private IntPtr _publisher;

        public short Token => GetCore().Version;

        public Box(in TStateMachine stateMachine) :
            base(stateMachine) => StateMachine.Initialize(this);

        public ValueTaskSourceStatus GetStatus(short token) =>
            GetCore().GetStatus(token);

        public TResult GetResult(short token) =>
            GetCore().GetResult(token);

        public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags) =>
            GetCore().OnCompleted(continuation, state, token, flags);

        public void Initialize(ref ValueTaskStateMachine publisher) =>
            _publisher = OffsetTo<TResult, ValueTaskStateMachine>(ref publisher);

        private ref ManualResetValueTaskSourceCore<TResult> GetCore() =>
            ref ReferenceTo<TResult, ValueTaskStateMachine>(_publisher)._core;
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
        internal ManualResetValueTaskSourceCore<TResult> _core;

        public StateMachineBox Box =>
            _box ?? throw new InvalidOperationException();

        public StateMachine<TResult> Reference =>
            StateMachine<TResult>.Create(ref this);

        public StateMachine? Continuation =>
            null;

        public void Dispose() { }

        public void Initialize(StateMachineBox box)
        {
            ((IBox) box).Initialize(ref this);

            _box = box;
        }

        public void OnCompleted() { }
        public void OnError(Exception error) => _core.SetException(error);
        public void OnNext(TResult value) => _core.SetResult(value);
    }
}