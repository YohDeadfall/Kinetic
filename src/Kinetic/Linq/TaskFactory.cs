using System;
using System.Threading.Tasks;
using Kinetic.Runtime;

namespace Kinetic.Linq;

internal readonly struct TaskFactory<TResult> : IStateMachineBoxFactory<Task<TResult>>
{
    public static Task<TResult> Create<TOperator>(in Operator<TOperator, TResult> source)
        where TOperator : IOperator<TResult>
    {
        return source.Build<Task<TResult>, TaskFactory<TResult>, StateMachine>(
            new TaskFactory<TResult>(), new StateMachine());
    }

    public Task<TResult> Create<TSource, TStateMachine>(TStateMachine stateMachine)
        where TStateMachine : struct, IEntryStateMachine<TSource>
    {
        var box = new Box<TSource, TStateMachine>(stateMachine);
        return box.TaskSource.Task; ;
    }

    private interface IBox
    {
        TaskCompletionSource<TResult> TaskSource { get; }
    }

    private sealed class Box<TSource, TStateMachine> : StateMachineBox<TSource, TStateMachine>, IBox
        where TStateMachine : struct, IEntryStateMachine<TSource>
    {
        public Box(TStateMachine stateMachine) :
            base(stateMachine)
        {
            StateMachine.Initialize(this);
            stateMachine.Start();
        }

        public TaskCompletionSource<TResult> TaskSource { get; } = new();
    }

    private struct StateMachine : IStateMachine<TResult>
    {
        private StateMachineBox? _box;

        public StateMachineBox Box =>
            _box ?? throw new InvalidOperationException();

        public StateMachineReference<TResult> Reference =>
            StateMachineReference<TResult>.Create(ref this);

        public StateMachineReference? Continuation =>
            null;

        public void Dispose() { }

        public void Initialize(StateMachineBox box) =>
            _box = box;

        public void OnCompleted()
        {
            var taskSource = GetTaskSource();
            if (taskSource.Task.IsCompleted)
                return;

            try
            {
                throw new InvalidOperationException("No result");
            }
            catch (Exception error)
            {
                taskSource.SetException(error);
            }
        }

        public void OnError(Exception error) =>
            GetTaskSource().SetException(error);

        public void OnNext(TResult value) =>
            GetTaskSource().SetResult(value);

        private TaskCompletionSource<TResult> GetTaskSource() =>
            ((IBox) Box).TaskSource;
    }
}