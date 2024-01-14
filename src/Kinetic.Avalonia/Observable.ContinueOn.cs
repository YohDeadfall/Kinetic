using System;
using Avalonia.Threading;
using Kinetic.Linq.StateMachines;

namespace Kinetic.Linq;

public static class Observable
{
    public static ObserverBuilder<TSource> ContinueOn<TSource>(this IObservable<TSource> source, Dispatcher dispatcher) =>
        source.ContinueOn(dispatcher, DispatcherPriority.Default);

    public static ObserverBuilder<TSource> ContinueOn<TSource>(this ObserverBuilder<TSource> source, Dispatcher dispatcher) =>
        source.ContinueOn(dispatcher, DispatcherPriority.Default);

    public static ObserverBuilder<TSource> ContinueOn<TSource>(this IObservable<TSource> source, Dispatcher dispatcher, DispatcherPriority priority) =>
        source.ToBuilder().ContinueOn(dispatcher, priority);

    public static ObserverBuilder<TSource> ContinueOn<TSource>(this ObserverBuilder<TSource> source, Dispatcher dispatcher, DispatcherPriority priority) =>
        source.ContinueWith<ContinueOnDispatcherStateMachineFactory<TSource>, TSource>(new(dispatcher, priority));

    private readonly struct ContinueOnDispatcherStateMachineFactory<TSource> : IStateMachineFactory<TSource, TSource>
    {
        private readonly Dispatcher _dispatcher;
        private readonly DispatcherPriority _priority;

        public ContinueOnDispatcherStateMachineFactory(Dispatcher dispatcher, DispatcherPriority priorty)
        {
            _dispatcher = dispatcher;
            _priority = priorty;
        }

        public void Create<TContinuation>(in TContinuation continuation, ObserverStateMachine<TSource> source)
            where TContinuation : struct, IStateMachine<TSource>
        {
            source.ContinueWith<ContinueOnDispatcherStateMachine<TContinuation, TSource>>(new(continuation, _dispatcher, _priority));
        }
    }

    private struct ContinueOnDispatcherStateMachine<TContinuation, TSource> : IStateMachine<TSource>
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
}