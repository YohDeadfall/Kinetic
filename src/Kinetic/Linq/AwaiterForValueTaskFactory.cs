using System;
using System.Threading.Tasks;

namespace Kinetic.Linq;

internal readonly struct AwaiterForValueTaskFactory :
    IAwaiterFactory<AwaiterForValueTask>
{
    private readonly Func<ValueTask> _factory;

    public AwaiterForValueTaskFactory(Func<ValueTask> factory) =>
        _factory = factory;

    public AwaiterForValueTask GetAwaiter() =>
        new(_factory().GetAwaiter());
}

internal readonly struct AwaiterForValueTaskFactory<T> :
    IAwaiterFactory<AwaiterForValueTask, T>,
    ITransform<T, AwaiterForValueTask>
{
    private readonly Func<T, ValueTask> _factory;

    public AwaiterForValueTaskFactory(Func<T, ValueTask> factory) =>
        _factory = factory;

    public AwaiterForValueTask GetAwaiter(T value) =>
        new(_factory(value).GetAwaiter());

    public AwaiterForValueTask Transform(T value) =>
        GetAwaiter(value);
}

internal readonly struct AwaiterForValueTaskFactory<T, TResult> :
    IAwaiterFactory<AwaiterForValueTask<TResult>, T, TResult>,
    ITransform<T, AwaiterForValueTask<TResult>>
{
    private readonly Func<T, ValueTask<TResult>> _factory;

    public AwaiterForValueTaskFactory(Func<T, ValueTask<TResult>> factory) =>
        _factory = factory;

    public AwaiterForValueTask<TResult> GetAwaiter(T value) =>
        new(_factory(value).GetAwaiter());

    public AwaiterForValueTask<TResult> Transform(T value) =>
        GetAwaiter(value);
}

internal readonly struct AwaiterForValueTaskFactory<T, TResult, TTransform> :
    IAwaiterFactory<AwaiterForValueTask<TResult>, T, TResult>,
    ITransform<T, AwaiterForValueTask<TResult>>
    where TTransform : struct, ITransform<T, ValueTask<TResult>>
{
    private readonly TTransform _transfrom;

    public AwaiterForValueTaskFactory(TTransform transform) =>
        _transfrom = transform;

    public AwaiterForValueTask<TResult> GetAwaiter(T value) =>
        new(_transfrom.Transform(value).GetAwaiter());

    public AwaiterForValueTask<TResult> Transform(T value) =>
        GetAwaiter(value);
}