using System;
using System.Threading.Tasks;

namespace Kinetic.Linq;

internal readonly struct AwaiterForTaskFactory :
    IAwaiterFactory<AwaiterForTask>
{
    private readonly Func<Task> _factory;

    public AwaiterForTaskFactory(Func<Task> factory) =>
        _factory = factory;

    public AwaiterForTask GetAwaiter() =>
        new(_factory().GetAwaiter());
}

internal readonly struct AwaiterForTaskFactory<T> :
    IAwaiterFactory<AwaiterForTask, T>,
    ITransform<T, AwaiterForTask>
{
    private readonly Func<T, Task> _factory;

    public AwaiterForTaskFactory(Func<T, Task> factory) =>
        _factory = factory;

    public AwaiterForTask GetAwaiter(T value) =>
        new(_factory(value).GetAwaiter());

    public AwaiterForTask Transform(T value) =>
        GetAwaiter(value);
}

internal readonly struct AwaiterForTaskFactory<T, TResult> :
    IAwaiterFactory<AwaiterForTask<TResult>, T, TResult>,
    ITransform<T, AwaiterForTask<TResult>>
{
    private readonly Func<T, Task<TResult>> _factory;

    public AwaiterForTaskFactory(Func<T, Task<TResult>> factory) =>
        _factory = factory;

    public AwaiterForTask<TResult> GetAwaiter(T value) =>
        new(_factory(value).GetAwaiter());

    public AwaiterForTask<TResult> Transform(T value) =>
        GetAwaiter(value);
}