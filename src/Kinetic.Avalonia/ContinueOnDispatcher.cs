using System;
using System.Runtime.InteropServices;
using Avalonia.Threading;
using Kinetic.Runtime;

namespace Kinetic.Linq;

[StructLayout(LayoutKind.Auto)]
public readonly struct ContinueOnDispatcher<TOperator, TSource> : IOperator<TSource>
    where TOperator : IOperator<TSource>
{
    private readonly TOperator _source;
    private readonly Dispatcher _dispatcher;
    private readonly DispatcherPriority _priority;

    public ContinueOnDispatcher(TOperator source, Dispatcher dispatcher, DispatcherPriority priority)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        _priority = priority;
    }

    public TBox Build<TBox, TBoxFactory, TContinuation>(in TBoxFactory boxFactory, TContinuation continuation)
        where TBoxFactory : struct, IStateMachineBoxFactory<TBox>
        where TContinuation : struct, IStateMachine<TSource>
    {
        return _source.Build<TBox, TBoxFactory, ContinueOnDispatcherStateMachine<TContinuation, TSource>>(
            boxFactory, new(continuation, _dispatcher, _priority));
    }
}