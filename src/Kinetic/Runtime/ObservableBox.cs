using System;

namespace Kinetic.Runtime;

internal sealed class ObservableBox<TSource, TResult, TStateMachine> : PublishingBox<TSource, TResult, TStateMachine>, IObservableInternal<TResult>
    where TStateMachine : struct, IStateMachine<TSource>
{
    private ObservableSubscriptions<TResult> _subscriptions;

    public ObservableBox(in TStateMachine stateMachine) :
        base(stateMachine)
    {
    }

    public static ObservableBox<TSource, TResult, TStateMachine> Create(in TStateMachine stateMachine) =>
        new(stateMachine);

    public void Subscribe(ObservableSubscription<TResult> subscription) =>
        _subscriptions.Subscribe(subscription, this);

    public IDisposable Subscribe(IObserver<TResult> observer) =>
        _subscriptions.Subscribe(observer, this);

    public void Unsubscribe(ObservableSubscription<TResult> subscription) =>
        _subscriptions.Unsubscribe(subscription);

    protected override void Complete() =>
        _subscriptions.OnCompleted();

    protected override void Error(Exception error) =>
        _subscriptions.OnError(error);

    protected override void Next(TResult value) =>
        _subscriptions.OnNext(value);
}