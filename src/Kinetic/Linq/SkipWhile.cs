using System;
using System.Runtime.InteropServices;
using Kinetic.Runtime;

namespace Kinetic.Linq;

[StructLayout(LayoutKind.Auto)]
public readonly struct SkipWhile<TOperator, TSource> : IOperator<TSource>
    where TOperator : IOperator<TSource>
{
    private readonly TOperator _source;
    private readonly Func<TSource, bool> _predicate;

    public SkipWhile(TOperator source, Func<TSource, bool> predicate)
    {
        _source = source.ThrowIfArgumentNull();
        _predicate = predicate.ThrowIfArgumentNull();
    }

    public TBox Build<TBox, TBoxFactory, TContinuation>(in TBoxFactory boxFactory, TContinuation continuation)
        where TBoxFactory : struct, IStateMachineBoxFactory<TBox>
        where TContinuation : struct, IStateMachine<TSource>
    {
        return _source.Build<TBox, TBoxFactory, FilterStateMachine<TContinuation, SkipWhileFilter<TSource>, TSource>>(
            boxFactory, new(continuation, new(_predicate)));
    }
}