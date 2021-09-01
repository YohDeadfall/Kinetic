namespace Kinetic
{
    internal interface IKineticFunction<T, TResult>
    {
        TResult Invoke(T value);
    }

    internal interface IKineticFunction<T1, T2, TResult>
    {
        TResult Invoke(T1 value1, T2 value2);
    }
}
