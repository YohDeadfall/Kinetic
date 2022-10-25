using System;
using System.Runtime.CompilerServices;

namespace Kinetic.Linq.StateMachines;

internal sealed class ObserverStateMachineBox<TSource, TStateMachine> : IObserver<TSource>, IObserverStateMachineBox
    where TStateMachine : struct, IObserverStateMachine<TSource>
{
    private TStateMachine _stateMachine;

    public ObserverStateMachineBox(in TStateMachine stateMachine)
    {
        try
        {
            _stateMachine = stateMachine;
            _stateMachine.Initialize(this);
        }
        catch
        {
            _stateMachine.Dispose();
            throw;
        }
    }

    public IDisposable Subscribe<T, TStateMachinePart>(IObservable<T> observable, in TStateMachinePart stateMachine)
        where TStateMachinePart : struct, IObserverStateMachine<T>
    {
        return observable.Subscribe(
            state: (self: this, offset: Observer.GetStateMachineOffset(_stateMachine, stateMachine)),
            onNext: static (state, value) =>
            {
                Observer
                    .GetStateMachine<TStateMachine, TStateMachinePart>(state.self._stateMachine, state.offset)
                    .OnNext(value);
            },
            onError: static (state, error) =>
            {
                Observer
                    .GetStateMachine<TStateMachine, TStateMachinePart>(state.self._stateMachine, state.offset)
                    .OnError(error);
            },
            onCompleted: static (state) =>
            {
                Observer
                    .GetStateMachine<TStateMachine, TStateMachinePart>(state.self._stateMachine, state.offset)
                    .OnCompleted();
            });
    }

    public void Dispose() => _stateMachine.Dispose();
    void IObserver<TSource>.OnNext(TSource value) => _stateMachine.OnNext(value);
    void IObserver<TSource>.OnError(Exception error) => _stateMachine.OnError(error);
    void IObserver<TSource>.OnCompleted() => _stateMachine.OnCompleted();
}

internal struct ObserverStateMachineBoxFactory : IObserverFactory<IDisposable>
{
    public IDisposable Create<TSource, TStateMachine>(in TStateMachine stateMachine)
        where TStateMachine : struct, IObserverStateMachine<TSource>
    {
        return new ObserverStateMachineBox<TSource, TStateMachine>(stateMachine);
    }
}