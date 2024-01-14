using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Kinetic.Linq;

interface IAwaiter
{
    bool IsCompleted { get; }
    void OnCompleted(Action continuation);

    void GetResult();
}

interface IAwaiter<T>
{
    bool IsCompleted { get; }
    void OnCompleted(Action continuation);

    T GetResult();
}

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

interface IAwaiterFactory<TAwaiter>
    where TAwaiter : struct, IAwaiter
{
    TAwaiter GetAwaiter();
}

interface IAwaiterFactory<TAwaiter, TSource>
    where TAwaiter : struct, IAwaiter
{
    TAwaiter GetAwaiter(TSource value);
}

interface IAwaiterFactory<TAwaiter, TSource, TResult>
    where TAwaiter : struct, IAwaiter<TResult>
{
    TAwaiter GetAwaiter(TSource value);
}

internal readonly struct AwaiterFactoryForTask :
    IAwaiterFactory<AwaiterForTask>
{
    private readonly Func<Task> _factory;

    public AwaiterFactoryForTask(Func<Task> factory) =>
        _factory = factory;

    public AwaiterForTask GetAwaiter() =>
        new(_factory().GetAwaiter());
}

internal readonly struct AwaiterFactoryForTask<TSource> :
    IAwaiterFactory<AwaiterForTask, TSource>
{
    private readonly Func<TSource, Task> _factory;

    public AwaiterFactoryForTask(Func<TSource, Task> factory) =>
        _factory = factory;

    public AwaiterForTask GetAwaiter(TSource value) =>
        new(_factory(value).GetAwaiter());
}

internal readonly struct AwaiterFactoryForTask<TSource, TResult> :
    IAwaiterFactory<AwaiterForTask<TResult>, TSource, TResult>
{
    private readonly Func<TSource, Task<TResult>> _factory;

    public AwaiterFactoryForTask(Func<TSource, Task<TResult>> factory) =>
        _factory = factory;

    public AwaiterForTask<TResult> GetAwaiter(TSource value) =>
        new(_factory(value).GetAwaiter());
}

internal readonly struct AwaiterFactoryForValueTask :
    IAwaiterFactory<AwaiterForValueTask>
{
    private readonly Func<ValueTask> _factory;

    public AwaiterFactoryForValueTask(Func<ValueTask> factory) =>
        _factory = factory;

    public AwaiterForValueTask GetAwaiter() =>
        new(_factory().GetAwaiter());
}

internal readonly struct AwaiterFactoryForValueTask<TSource> :
    IAwaiterFactory<AwaiterForValueTask, TSource>
{
    private readonly Func<TSource, ValueTask> _factory;

    public AwaiterFactoryForValueTask(Func<TSource, ValueTask> factory) =>
        _factory = factory;

    public AwaiterForValueTask GetAwaiter(TSource value) =>
        new(_factory(value).GetAwaiter());
}

internal readonly struct AwaiterFactoryForValueTask<TSource, TResult> :
    IAwaiterFactory<AwaiterForValueTask<TResult>, TSource, TResult>
{
    private readonly Func<TSource, ValueTask<TResult>> _factory;

    public AwaiterFactoryForValueTask(Func<TSource, ValueTask<TResult>> factory) =>
        _factory = factory;

    public AwaiterForValueTask<TResult> GetAwaiter(TSource value) =>
        new(_factory(value).GetAwaiter());
}