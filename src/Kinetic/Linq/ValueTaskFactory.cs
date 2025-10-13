using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using Kinetic.Runtime;

namespace Kinetic.Linq;

internal readonly struct ValueTaskFactory<TResult> : IStateMachineBoxFactory<ValueTask<TResult>>
{
    public static ValueTask<TResult> Create<TOperator>(in Operator<TOperator, TResult> source)
        where TOperator : IOperator<TResult>
    {
        return source.Build<ValueTask<TResult>, ValueTaskFactory<TResult>, StateMachine>(
            new ValueTaskFactory<TResult>(), new StateMachine());
    }

    public ValueTask<TResult> Create<TSource, TStateMachine>(TStateMachine stateMachine)
        where TStateMachine : struct, IEntryStateMachine<TSource>
    {
        var box = new Box<TSource, TStateMachine>(stateMachine);
        return new(box, box.Token);
    }

    private interface IBox : IValueTaskSource<TResult>
    {
        void Initialize(ref StateMachine publisher);
    }

    private sealed class Box<T, TStateMachine> : StateMachineBox<T, TStateMachine>, IBox
        where TStateMachine : struct, IEntryStateMachine<T>
    {
        private IntPtr _publisher;

        public short Token => GetTask().Version;

        public Box(in TStateMachine stateMachine) :
            base(stateMachine)
        {
            StateMachine.Initialize(this);
            StateMachine.Start();
        }

        public ValueTaskSourceStatus GetStatus(short token) =>
            GetTask().GetStatus(token);

        public TResult GetResult(short token) =>
            GetTask().GetResult(token);

        public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags) =>
            GetTask().OnCompleted(continuation, state, token, flags);

        public void Initialize(ref StateMachine publisher) =>
            _publisher = OffsetTo<TResult, StateMachine>(ref publisher);

        private ref ManualResetValueTaskSourceCore<TResult> GetTask() =>
            ref ReferenceTo<TResult, StateMachine>(_publisher)._task;
    }

    internal struct StateMachine : IStateMachine<TResult>
    {
        private StateMachineBox? _box;
        internal ManualResetValueTaskSourceCore<TResult> _task;

        public StateMachineBox Box =>
            _box ?? throw new InvalidOperationException();

        public StateMachineReference<TResult> Reference =>
            StateMachineReference<TResult>.Create(ref this);

        public StateMachineReference? Continuation =>
            null;

        public void Dispose() { }

        public void Initialize(StateMachineBox box)
        {
            ((IBox) box).Initialize(ref this);

            _box = box;
        }

        public void OnCompleted()
        {
            if (_task.GetStatus(_task.Version) != ValueTaskSourceStatus.Pending)
            {
                _box!.Dispose();
                return;
            }

            try
            {
                throw new InvalidOperationException("No result");
            }
            catch (Exception error)
            {
                OnError(error);
            }
        }

        public void OnError(Exception error)
        {
            _task.SetException(error);
            _box!.Dispose();
        }

        public void OnNext(TResult value)
        {
            _task.SetResult(value);
        }
    }
}