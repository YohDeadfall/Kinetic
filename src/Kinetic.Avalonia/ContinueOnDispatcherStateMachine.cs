using System;
using Avalonia.Threading;
using Kinetic.Runtime;

namespace Kinetic.Linq;

internal struct ContinueOnDispatcherStateMachine<TContinuation, TSource> : IStateMachine<TSource>
    where TContinuation : struct, IStateMachine<TSource>
{
    private TContinuation _continuation;
    private readonly Dispatcher _dispatcher;
    private readonly DispatcherPriority _priority;

    public ContinueOnDispatcherStateMachine(in TContinuation continuation, Dispatcher dispatcher, DispatcherPriority priorty)
    {
        _continuation = continuation;
        _dispatcher = dispatcher;
        _priority = priorty;
    }

    public StateMachineBox Box =>
        _continuation.Box;

    public StateMachineReference<TSource> Reference =>
        new StateMachineReference<TSource, ContinueOnDispatcherStateMachine<TContinuation, TSource>>(ref this);

    public StateMachineReference? Continuation =>
        _continuation.Reference;

    public void Initialize(StateMachineBox box) =>
        _continuation.Initialize(box);

    public void Dispose() =>
        _continuation.Dispose();

    public void OnNext(TSource value)
    {
        if (_dispatcher.CheckAccess())
            _continuation.OnNext(value);
        else
        {
            var reference = new StateMachineReference<TSource, TContinuation>(ref _continuation);
            var continuation = () => reference.Target.OnNext(value);

            _dispatcher.Post(continuation, _priority);
        }
    }

    public void OnError(Exception error)
    {
        if (_dispatcher.CheckAccess())
            _continuation.OnError(error);
        else
        {
            var reference = new StateMachineReference<TSource, TContinuation>(ref _continuation);
            var continuation = () => reference.Target.OnError(error);

            _dispatcher.Post(continuation, _priority);
        }
    }

    public void OnCompleted()
    {
        if (_dispatcher.CheckAccess())
            _continuation.OnCompleted();
        else
        {
            var reference = new StateMachineReference<TSource, TContinuation>(ref _continuation);
            var continuation = () => reference.Target.OnCompleted();

            _dispatcher.Post(continuation, _priority);
        }
    }
}