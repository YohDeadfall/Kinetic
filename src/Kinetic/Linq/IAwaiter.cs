using System;

namespace Kinetic.Linq;

internal interface IAwaiter
{
    bool IsCompleted { get; }
    void OnCompleted(Action continuation);

    void GetResult();
}

internal interface IAwaiter<T>
{
    bool IsCompleted { get; }
    void OnCompleted(Action continuation);

    T GetResult();
}