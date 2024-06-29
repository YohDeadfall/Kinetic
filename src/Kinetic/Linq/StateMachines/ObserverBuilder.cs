using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Kinetic.Linq.StateMachines;

public static class ObserverBuilder
{
    public static ObserverBuilder<T> ToBuilder<T>(this IObservable<T> source) =>
        ObserverBuilder<T>.Create(source);
}

public readonly struct ObserverBuilder<T>
{
    private readonly ObserverBuilderStep<T> _outer;
    private readonly ObserverBuilderStep _inner;

    private ObserverBuilder(ObserverBuilderStep<T> outer, ObserverBuilderStep inner)
    {
        _outer = outer;
        _inner = inner;
    }

    public static ObserverBuilder<T> Create(IObservable<T> source)
    {
        var step = new ObserverBuilderStateMachineStep<T, T, ObserverStateMachineFactory<T>> { StateMachine = new(source) };
        var builder = new ObserverBuilder<T>(step, step);

        return builder;
    }

    public ObserverBuilder<TResult> ContinueWith<TStateMachine, TResult>(scoped in TStateMachine stateMachine)
        where TStateMachine : struct, IStateMachineFactory<T, TResult>
    {
        var step = new ObserverBuilderStateMachineStep<T, TResult, TStateMachine> { StateMachine = stateMachine, Next = _outer };
        var builder = new ObserverBuilder<TResult>(step, _inner ?? step);

        return builder;
    }

    public TObserver Build<TContinuation, TFactory, TObserver>(scoped in TContinuation continuation, in TFactory factory)
        where TContinuation : struct, IStateMachine<T>
        where TFactory : struct, IStateMachineBoxFactory<TObserver>
    {
        var stateMachine = (IObserverBuilderStateMachineStep) _inner;
        var observer = stateMachine.UseFactory<TFactory, TObserver>(factory);

        _outer.ContinueWith(continuation);

        return observer.Observer;
    }
}

public ref struct ObserverStateMachine<TResult>
{
    private readonly ObserverBuilderStep<TResult> _builder;

    internal ObserverStateMachine(ObserverBuilderStep<TResult> builder) =>
        _builder = builder;

    public void ContinueWith<TContinuation>(in TContinuation continuation)
        where TContinuation : struct, IStateMachine<TResult>
    {
        _builder.ContinueWith(continuation);
    }
}

internal abstract class ObserverBuilderStep
{
    internal ObserverBuilderStep? Next;
}

internal abstract class ObserverBuilderStep<TResult> : ObserverBuilderStep
{
    public abstract void ContinueWith<TContinuation>(in TContinuation continuation)
        where TContinuation : struct, IStateMachine<TResult>;

    public static implicit operator ObserverStateMachine<TResult>(ObserverBuilderStep<TResult> builder) =>
        new(builder);
}

internal sealed class ObserverBuilderStateMachineStep<T, TResult, TStateMachine> : ObserverBuilderStep<TResult>, IObserverBuilderStateMachineStep
    where TStateMachine : struct, IStateMachineFactory<T, TResult>
{
    public TStateMachine StateMachine;

    public override void ContinueWith<TContinuation>(in TContinuation continuation)
    {
        Debug.Assert(Next is ObserverBuilderStep<T>);
        StateMachine.Create(continuation, Unsafe.As<ObserverBuilderStep<T>>(Next));
    }

    public IObserverBuilderFactoryStep<TObserver> UseFactory<TFactory, TObserver>(in TFactory factory)
        where TFactory : struct, IStateMachineBoxFactory<TObserver>
    {
        var result = new ObserverBuilderFactoryStep<T, TFactory, TObserver> { Factory = factory };

        Debug.Assert(Next is null);
        Next = result;

        return result;
    }
}

internal sealed class ObserverBuilderFactoryStep<T, TFactory, TObserver> : ObserverBuilderStep<T>, IObserverBuilderFactoryStep<TObserver>
    where TFactory : struct, IStateMachineBoxFactory<TObserver>
{
    [AllowNull]
    public TObserver Observer { get; private set; }
    public TFactory Factory;

    public override void ContinueWith<TContinuation>(in TContinuation continuation)
    {
        Debug.Assert(Next is null);
        Observer = Factory.Create<T, TContinuation>(continuation);
    }
}

internal interface IObserverBuilderStateMachineStep
{
    IObserverBuilderFactoryStep<TObserver> UseFactory<TFactory, TObserver>(in TFactory factory)
        where TFactory : struct, IStateMachineBoxFactory<TObserver>;
}

internal interface IObserverBuilderFactoryStep<TObserver>
{
    TObserver Observer { get; }
}