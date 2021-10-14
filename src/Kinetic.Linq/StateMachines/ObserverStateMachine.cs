using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Kinetic.Linq.StateMachines
{
    public interface IObserverStateMachine<TSource> : IObserver<TSource>, IDisposable
    {
        void Initialize(IObserverStateMachineBox box);
    }

    public interface IObserverStateMachineFactory<TSource, TResult>
    {
        void Create<TContinuation>(in TContinuation continuation, ObserverStateMachine<TSource> source)
            where TContinuation : struct, IObserverStateMachine<TResult>;
    }

    public interface IObserverStateMachineBox : IDisposable
    {
        IDisposable Subscribe<TSource, TStateMachine>(IObservable<TSource> observable, in TStateMachine stateMachine)
            where TStateMachine : struct, IObserverStateMachine<TSource>;
    }

    public interface IObserverFactory<TObserver>
    {
        TObserver Create<TSource, TStateMachine>(in TStateMachine stateMachine)
            where TStateMachine : struct, IObserverStateMachine<TSource>;
    }

    public ref struct ObserverStateMachine<TResult>
    {
        private readonly ObserverBuilderStep<TResult> _builder;

        internal ObserverStateMachine(ObserverBuilderStep<TResult> builder) =>
            _builder = builder;

        public void ContinueWith<TContinuation>(in TContinuation continuation)
            where TContinuation : struct, IObserverStateMachine<TResult>
        {
            _builder.ContinueWith(continuation);
        }
    }

    public static class ObserverBuilder
    {
        public static ObserverBuilder<TSource> ToBuilder<TSource>(this IObservable<TSource> source) =>
            ObserverBuilder<TSource>.Create(source);
    }

    public ref struct ObserverBuilder<TSource>
    {
        private ObserverBuilderStep<TSource> _outer;
        private ObserverBuilderStep _inner;

        public static ObserverBuilder<TSource> Create(IObservable<TSource> source)
        {
            var step = new ObserverBuilderStateMachineStep<TSource, TSource, ObserverStateMachineFactory<TSource>> { StateMachine = new(source) };
            var builder = new ObserverBuilder<TSource> { _outer = step, _inner = step };

            return builder;
        }

        public ObserverBuilder<TResult> ContinueWith<TStateMachine, TResult>(in TStateMachine stateMachine)
            where TStateMachine : struct, IObserverStateMachineFactory<TSource, TResult>
        {
            var step = new ObserverBuilderStateMachineStep<TSource, TResult, TStateMachine> { StateMachine = stateMachine, Next = _outer };
            var builder = new ObserverBuilder<TResult> { _outer = step, _inner = _inner ?? step };

            return builder;
        }

        public TObserver Build<TContinuation, TFactory, TObserver>(in TContinuation continuation, in TFactory factory)
            where TContinuation : struct, IObserverStateMachine<TSource>
            where TFactory : struct, IObserverFactory<TObserver>
        {
            var stateMachine = (IObserverBuilderStateMachineStep) _inner;
            var observer = stateMachine.UseFactory<TFactory, TObserver>(factory);

            _outer.ContinueWith(continuation);

            return observer.Observer;
        }
    }

    internal abstract class ObserverBuilderStep
    {
        internal ObserverBuilderStep? Next;
    }

    internal abstract class ObserverBuilderStep<TResult> : ObserverBuilderStep
    {
        public abstract void ContinueWith<TContinuation>(in TContinuation continuation)
            where TContinuation : struct, IObserverStateMachine<TResult>;

        public static implicit operator ObserverStateMachine<TResult>(ObserverBuilderStep<TResult> builder) =>
            new(builder);
    }

    internal sealed class ObserverBuilderStateMachineStep<TSource, TResult, TStateMachine> : ObserverBuilderStep<TResult>, IObserverBuilderStateMachineStep
        where TStateMachine : struct, IObserverStateMachineFactory<TSource, TResult>
    {
        public TStateMachine StateMachine;

        public override void ContinueWith<TContinuation>(in TContinuation continuation)
        {
            Debug.Assert(Next is ObserverBuilderStep<TSource>);
            StateMachine.Create(continuation, Unsafe.As<ObserverBuilderStep<TSource>>(Next));
        }

        public IObserverBuilderFactoryStep<TObserver> UseFactory<TFactory, TObserver>(in TFactory factory)
            where TFactory : struct, IObserverFactory<TObserver>
        {
            var result = new ObserverBuilderFactoryStep<TSource, TFactory, TObserver> { Factory = factory };

            Debug.Assert(Next is null);
            Next = result;

            return result;
        }
    }

    internal sealed class ObserverBuilderFactoryStep<TSource, TFactory, TObserver> : ObserverBuilderStep<TSource>, IObserverBuilderFactoryStep<TObserver>
        where TFactory : struct, IObserverFactory<TObserver>
    {
        [AllowNull]
        public TObserver Observer { get; private set; }
        public TFactory Factory;

        public override void ContinueWith<TContinuation>(in TContinuation continuation)
        {
            Debug.Assert(Next is null);
            Observer = Factory.Create<TSource, TContinuation>(continuation);
        }
    }

    internal interface IObserverBuilderStateMachineStep
    {
        IObserverBuilderFactoryStep<TObserver> UseFactory<TFactory, TObserver>(in TFactory factory)
            where TFactory : struct, IObserverFactory<TObserver>;
    }

    internal interface IObserverBuilderFactoryStep<TObserver>
    {
        TObserver Observer { get; }
    }

    internal struct ObserverStateMachine<TStateMachine, T> : IObserverStateMachine<T>
        where TStateMachine : struct, IObserverStateMachine<T>
    {
        private TStateMachine _stateMachine;
        private IObservable<T>? _observable;
        private IDisposable? _subscription;

        public ObserverStateMachine(in TStateMachine stateMachine, IObservable<T> observable)
        {
            _stateMachine = stateMachine;
            _observable = observable;
            _subscription = null;
        }

        public void Initialize(IObserverStateMachineBox box)
        {
            _stateMachine.Initialize(box);
            _subscription = _observable!.Subscribe((IObserver<T>) box);

            if (_observable is null)
            {
                _subscription.Dispose();
                _subscription = null;
            }
        }

        public void Dispose()
        {
            _subscription?.Dispose();
            _subscription = null;
            _observable = null;

            _stateMachine.Dispose();
        }

        public void OnNext(T value)
        {
            _stateMachine.OnNext(value);
        }

        public void OnError(Exception error)
        {
            _subscription?.Dispose();
            _subscription = null;
            _observable = null;

            _stateMachine.OnError(error);
            _stateMachine.Dispose();
        }

        public void OnCompleted()
        {
            _subscription?.Dispose();
            _subscription = null;
            _observable = null;

            _stateMachine.OnCompleted();
            _stateMachine.Dispose();
        }
    }

    internal struct ObserverStateMachineFactory<T> : IObserverStateMachineFactory<T, T>
    {
        private readonly IObservable<T> _observable;

        public ObserverStateMachineFactory(IObservable<T> observable) =>
            _observable = observable;

        public void Create<TContinuation>(in TContinuation continuation, ObserverStateMachine<T> source)
            where TContinuation : struct, IObserverStateMachine<T>
        {
            source.ContinueWith(new ObserverStateMachine<TContinuation, T>(continuation, _observable));
        }
    }
}