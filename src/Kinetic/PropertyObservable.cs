using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Kinetic.Linq.StateMachines;

namespace Kinetic;

internal abstract class PropertyObservable : StateMachineBox
{
    internal readonly IntPtr Offset;
    internal readonly ObservableObject Owner;
    internal readonly PropertyObservable? Next;

    internal uint Version;

    protected PropertyObservable(IntPtr offset, ObservableObject owner, PropertyObservable? next) =>
        (Owner, Offset, Next) = (owner, offset, next);

    internal abstract void Changed();
}

[DebuggerDisplay("Observers = {Subscriptions.GetObserversCountForDebugger()}")]
[DebuggerTypeProxy(typeof(PropertyObservableDebugView<>))]
internal abstract class PropertyObservable<T> : PropertyObservable, IObservableInternal<T>, IObserver<T>
{
    internal ObservableSubscriptions<T> Subscriptions;

    protected PropertyObservable(IntPtr offset, ObservableObject owner, PropertyObservable? next) :
        base(offset, owner, next)
    { }

    internal abstract void Changing(T value);

    internal override void Changed() =>
        Changed(Owner.Get<T>(Offset));

    internal void Changed(T value) =>
        Subscriptions.OnNext(value);

    public IDisposable Subscribe(IObserver<T> observer)
    {
        observer.OnNext(Owner.Get<T>(Offset));
        return Subscriptions.Subscribe(this, observer);
    }

    public void Subscribe(ObservableSubscription<T> subscription) =>
        Subscriptions.Subscribe(this, subscription);

    public void Unsubscribe(ObservableSubscription<T> subscription) =>
        Subscriptions.Unsubscribe(subscription);

    public void OnCompleted()
    { }

    public void OnError(Exception error)
    { }

    public void OnNext(T value) =>
        Owner.Set(Offset, value);
}

internal sealed class PropertyObservable<T, TStateMachine> : PropertyObservable<T>
    where TStateMachine : struct, IStateMachine<T>
{
    private TStateMachine _stateMachine;

    private protected sealed override ReadOnlySpan<byte> StateMachineData =>
        MemoryMarshal.CreateSpan(
            ref Unsafe.As<TStateMachine, byte>(ref _stateMachine),
            length: Unsafe.SizeOf<TStateMachine>());

    private ref TStateMachine StateMachine =>
        ref _stateMachine;

    public PropertyObservable(IntPtr offset, ObservableObject owner, PropertyObservable? next, in TStateMachine stateMachine) :
        base(offset, owner, next)
    {
        _stateMachine = stateMachine;
        _stateMachine.Initialize(this);
    }

    internal override void Changing(T value) =>
        _stateMachine.OnNext(value);
}

internal readonly struct PropertyObservableFactory : IStateMachineBoxFactory<PropertyObservable>
{
    private readonly IntPtr _offset;
    private readonly ObservableObject _owner;
    private readonly PropertyObservable? _next;

    public PropertyObservableFactory(
        IntPtr offset,
        ObservableObject owner,
        PropertyObservable? next)
    {
        _offset = offset;
        _owner = owner;
        _next = next;
    }

    public PropertyObservable Create<T, TStateMachine>(in TStateMachine stateMachine)
        where TStateMachine : struct, IStateMachine<T>
    {
        return new PropertyObservable<T, TStateMachine>(_offset, _owner, _next, stateMachine);
    }
}