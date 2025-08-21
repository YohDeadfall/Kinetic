using System;
using System.Runtime.InteropServices;
using Kinetic.Runtime;

namespace Kinetic.Linq;

[StructLayout(LayoutKind.Auto)]
public readonly struct Throttle<TOperator, TSource> : IOperator<TSource>
    where TOperator : IOperator<TSource>
{
    private readonly TOperator _source;
    private readonly TimeSpan _delay;
    private readonly bool _continueOnCapturedContext;

    public Throttle(TOperator source, TimeSpan delay, bool continueOnCapturedContext)
    {
        _source = source.ThrowIfArgumentNull();
        _delay = delay;
        _continueOnCapturedContext = continueOnCapturedContext;
    }

    public TBox Build<TBox, TBoxFactory, TContinuation>(in TBoxFactory boxFactory, TContinuation continuation)
        where TBoxFactory : struct, IStateMachineBoxFactory<TBox>
        where TContinuation : struct, IStateMachine<TSource>
    {
        return _source.Build<TBox, TBoxFactory, ThrottleStateMachine<TContinuation, TSource>>(
            boxFactory, new(continuation, _delay, _continueOnCapturedContext));
    }
}