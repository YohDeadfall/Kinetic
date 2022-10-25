using System;
using System.Runtime.CompilerServices;

namespace Kinetic.Linq.StateMachines;

internal abstract class ObservableStateMachineBox<T> : IObservableInternal<T>
{
    private ObservableSubscriptions<T> _subscriptions;

    public IDisposable Subscribe(IObserver<T> observer) =>
        _subscriptions.Subscribe(this, observer);

    void IObservableInternal<T>.Subscribe(ObservableSubscription<T> subscription) =>
        _subscriptions.Subscribe(this, subscription);

    void IObservableInternal<T>.Unsubscribe(ObservableSubscription<T> subscription) =>
        _subscriptions.Unsubscribe(subscription);

    public void OnNext(T value) => _subscriptions.OnNext(value);
    public void OnError(Exception error) => _subscriptions.OnError(error);
    public void OnCompleted() => _subscriptions.OnCompleted();
}

internal sealed class ObservableStateMachineBox<TResult, TSource, TStateMachine> : ObservableStateMachineBox<TResult>, IObserver<TSource>, IObserverStateMachineBox
    where TStateMachine : struct, IObserverStateMachine<TSource>
{
    private TStateMachine _stateMachine;

    public ObservableStateMachineBox(in TStateMachine stateMachine)
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

internal struct ObservableStateMachine<TResult> : IObserverStateMachine<TResult>
{
    private ObservableStateMachineBox<TResult> _observable;

    public void Initialize(IObserverStateMachineBox box) => _observable = (ObservableStateMachineBox<TResult>) box;
    public void Dispose() { }

    public void OnNext(TResult value) => _observable.OnNext(value);
    public void OnError(Exception error) => _observable.OnError(error);
    public void OnCompleted() => _observable.OnCompleted();
}

internal struct ObservableStateMachineBoxFactory<TResult> : IObserverFactory<ObservableStateMachineBox<TResult>>
{
    public ObservableStateMachineBox<TResult> Create<TSource, TStateMachine>(in TStateMachine stateMachine)
        where TStateMachine : struct, IObserverStateMachine<TSource>
    {
        return new ObservableStateMachineBox<TResult, TSource, TStateMachine>(stateMachine);
    }
}