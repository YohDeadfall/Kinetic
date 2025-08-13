using System;

namespace Kinetic.Benchmarks;

public sealed class NoOpObserver<T> : IObserver<T>
{
    public static readonly NoOpObserver<T> Instance = new();

    public void OnCompleted() { }

    public void OnError(Exception error) { }

    public void OnNext(T value) { }
}