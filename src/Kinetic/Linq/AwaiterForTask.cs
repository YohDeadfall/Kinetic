using System;
using System.Runtime.CompilerServices;

namespace Kinetic.Linq;

internal struct AwaiterForTask : IAwaiter
{
    private readonly TaskAwaiter _awaiter;

    public AwaiterForTask(TaskAwaiter awaiter) =>
        _awaiter = awaiter;

    public bool IsCompleted =>
        _awaiter.IsCompleted;

    public void OnCompleted(Action continuation) =>
        _awaiter.OnCompleted(continuation);

    public void GetResult() =>
        _awaiter.GetResult();
}

internal struct AwaiterForTask<T> : IAwaiter<T>
{
    private readonly TaskAwaiter<T> _awaiter;

    public AwaiterForTask(TaskAwaiter<T> awaiter) =>
        _awaiter = awaiter;

    public bool IsCompleted =>
        _awaiter.IsCompleted;

    public void OnCompleted(Action continuation) =>
        _awaiter.OnCompleted(continuation);

    public T GetResult() =>
        _awaiter.GetResult();
}
