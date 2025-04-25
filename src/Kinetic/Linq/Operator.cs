using Kinetic.Runtime;

namespace Kinetic.Linq;

public readonly struct Operator<TOperator, T>
    where TOperator : IOperator<T>
{
    private readonly TOperator _op;

    public Operator(TOperator op) =>
        _op = op.ThrowIfNull();

    public TBox Build<TBox, TBoxFactory, TContinuation>(in TBoxFactory boxFactory, TContinuation continuation)
        where TBoxFactory : struct, IStateMachineBoxFactory<TBox>
        where TContinuation : struct, IStateMachine<T>
    {
        return _op.Build<TBox, TBoxFactory, TContinuation>(boxFactory, continuation);
    }

    public Operator<Cast<TOperator, T, TResult>, TResult> Cast<TResult>() =>
        new(new(_op));

    public Operator<OfType<TOperator, T, TResult>, TResult> OfType<TResult>() =>
        new(new(_op));

    public static implicit operator TOperator(Operator<TOperator, T> op) =>
        op._op;
}
