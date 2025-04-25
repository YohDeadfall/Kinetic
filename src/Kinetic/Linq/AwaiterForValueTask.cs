using System;
using System.Runtime.CompilerServices;

namespace Kinetic.Linq;

internal struct AwaiterForValueTask : IAwaiter
{
    private readonly ValueTaskAwaiter _awaiter;

    public AwaiterForValueTask(ValueTaskAwaiter awaiter) =>
        _awaiter = awaiter;

    public bool IsCompleted =>
        _awaiter.IsCompleted;

    public void OnCompleted(Action continuation) =>
        _awaiter.OnCompleted(continuation);

    public void GetResult() =>
        _awaiter.GetResult();
}

internal struct AwaiterForValueTask<T> : IAwaiter<T>
{
    private readonly ValueTaskAwaiter<T> _awaiter;

    public AwaiterForValueTask(ValueTaskAwaiter<T> awaiter) =>
        _awaiter = awaiter;

    public bool IsCompleted =>
        _awaiter.IsCompleted;

    public void OnCompleted(Action continuation) =>
        _awaiter.OnCompleted(continuation);

    public T GetResult() =>
        _awaiter.GetResult();
}
