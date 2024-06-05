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
        var step = new ObserverBuilderStateMachineStep<T, T, StateMachineFactory> { StateMachine = new(source) };
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

    private struct StateMachine<TContinuation> : IStateMachine<T>
        where TContinuation : struct, IStateMachine<T>
    {
        private TContinuation _continuation;
        private IObservable<T>? _observable;
        private IDisposable? _subscription;

        public StateMachine(in TContinuation continuation, IObservable<T> observable)
        {
            _continuation = continuation;
            _observable = observable;
            _subscription = null;
        }

        public StateMachineBox Box =>
            _continuation.Box;

        public void Initialize(StateMachineBox box)
        {
            _continuation.Initialize(box);
            _subscription = _observable?.Subscribe(
                (StateMachineBox<T, StateMachine<TContinuation>>) box);
        }

        public void Dispose()
        {
            _subscription?.Dispose();
            _continuation.Dispose();
        }

        public void OnCompleted() =>
            _continuation.OnCompleted();

        public void OnError(Exception error) =>
            _continuation.OnError(error);

        public void OnNext(T value) =>
            _continuation.OnNext(value);
    }

    private readonly struct StateMachineFactory : IStateMachineFactory<T, T>
    {
        private readonly IObservable<T> _observable;

        public StateMachineFactory(IObservable<T> observable) =>
            _observable = observable;

        public void Create<TContinuation>(in TContinuation continuation, ObserverStateMachine<T> source)
            where TContinuation : struct, IStateMachine<T> =>
            source.ContinueWith(new StateMachine<TContinuation>(continuation, _observable));
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