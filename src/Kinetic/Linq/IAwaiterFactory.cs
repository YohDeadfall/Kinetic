namespace Kinetic.Linq;

interface IAwaiterFactory<TAwaiter>
    where TAwaiter : struct, IAwaiter
{
    TAwaiter GetAwaiter();
}

interface IAwaiterFactory<TAwaiter, T>
    where TAwaiter : struct, IAwaiter
{
    TAwaiter GetAwaiter(T value);
}

interface IAwaiterFactory<TAwaiter, T, TResult>
    where TAwaiter : struct, IAwaiter<TResult>
{
    TAwaiter GetAwaiter(T value);
}
