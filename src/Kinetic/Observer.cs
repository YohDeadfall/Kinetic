using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Kinetic
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

        public ObserverBuilder<TResult> ContinueWith<TResult, TStateMachine>(in TStateMachine stateMachine)
            where TStateMachine : struct, IObserverStateMachineFactory<TSource, TResult>
        {
            var step = new ObserverBuilderStateMachineStep<TSource, TResult, TStateMachine> { StateMachine = stateMachine, Next = _outer };
            var builder = new ObserverBuilder<TResult> { _outer = step, _inner = _inner };

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

    internal sealed class Observer<TSource, TStateMachine> : IObserver<TSource>, IObserverStateMachineBox, IDisposable
        where TStateMachine : struct, IObserverStateMachine<TSource>
    {
        private TStateMachine _stateMachine;

        public Observer(in TStateMachine stateMachine)
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
                state: (self: this, offset: GetStateMachineOffset(stateMachine)),
                onNext: static (state, value) =>
                {
                    state.self
                        .GetStateMachine<TStateMachinePart>(state.offset)
                        .OnNext(value);
                },
                onError: static (state, error) =>
                {
                    state.self
                        .GetStateMachine<TStateMachinePart>(state.offset)
                        .OnError(error);
                },
                onCompleted: static (state) =>
                {
                    state.self
                        .GetStateMachine<TStateMachinePart>(state.offset)
                        .OnCompleted();
                });
        }

        private ref TStateMachinePart GetStateMachine<TStateMachinePart>(IntPtr offset)
        {
            ref var stateMachine = ref Unsafe.As<TStateMachine, IntPtr>(ref _stateMachine);
            ref var stateMachinePart = ref Unsafe.As<IntPtr, TStateMachinePart>(
                ref Unsafe.AddByteOffset(ref stateMachine, offset));
            return ref stateMachinePart!;
        }

        private IntPtr GetStateMachineOffset<TStateMachinePart>(in TStateMachinePart stateMachine)
        {
            return Unsafe.ByteOffset(
                ref Unsafe.As<TStateMachine, IntPtr>(ref _stateMachine),
                ref Unsafe.As<TStateMachinePart, IntPtr>(ref Unsafe.AsRef(stateMachine)));
        }

        public void Dispose() => _stateMachine.Dispose();
        void IObserver<TSource>.OnNext(TSource value) => _stateMachine.OnNext(value);
        void IObserver<TSource>.OnError(Exception error) => _stateMachine.OnError(error);
        void IObserver<TSource>.OnCompleted() => _stateMachine.OnCompleted();
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

    internal struct ObserverFactory : IObserverFactory<IDisposable>
    {
        public IDisposable Create<TSource, TStateMachine>(in TStateMachine stateMachine)
            where TStateMachine : struct, IObserverStateMachine<TSource>
        {
            return new Observer<TSource, TStateMachine>(stateMachine);
        }
    }

    internal abstract class Observable<T> : IObservableInternal<T>
    {
        private ObservableSubscriptions<T> _subscriptions;

        public IDisposable Subscribe(IObserver<T> observer) =>
            _subscriptions.Subscribe(this, observer);

        public void Subscribe(ObservableSubscription<T> subscription) =>
            _subscriptions.Subscribe(this, subscription);

        public void Unsubscribe(ObservableSubscription<T> subscription) =>
            _subscriptions.Unsubscribe(subscription);

        public void OnNext(T value) => _subscriptions.OnNext(value);
        public void OnError(Exception error) => _subscriptions.OnError(error);
        public void OnCompleted() => _subscriptions.OnCompleted();
    }

    internal sealed class Observable<TResult, TSource, TStateMachine> : Observable<TResult>, IObserver<TSource>, IObserverStateMachineBox, IDisposable
        where TStateMachine : struct, IObserverStateMachine<TSource>
    {
        private TStateMachine _stateMachine;

        public Observable(in TStateMachine stateMachine)
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
                state: (self: this, offset: GetStateMachineOffset(stateMachine)),
                onNext: static (state, value) =>
                {
                    state.self
                        .GetStateMachine<TStateMachinePart>(state.offset)
                        .OnNext(value);
                },
                onError: static (state, error) =>
                {
                    state.self
                        .GetStateMachine<TStateMachinePart>(state.offset)
                        .OnError(error);
                },
                onCompleted: static (state) =>
                {
                    state.self
                        .GetStateMachine<TStateMachinePart>(state.offset)
                        .OnCompleted();
                });
        }

        private ref TStateMachinePart GetStateMachine<TStateMachinePart>(IntPtr offset)
        {
            ref var stateMachine = ref Unsafe.As<TStateMachine, IntPtr>(ref _stateMachine);
            ref var stateMachinePart = ref Unsafe.As<IntPtr, TStateMachinePart>(
                ref Unsafe.AddByteOffset(ref stateMachine, offset));
            return ref stateMachinePart!;
        }

        private IntPtr GetStateMachineOffset<TStateMachinePart>(in TStateMachinePart stateMachine)
        {
            return Unsafe.ByteOffset(
                ref Unsafe.As<TStateMachine, IntPtr>(ref _stateMachine),
                ref Unsafe.As<TStateMachinePart, IntPtr>(ref Unsafe.AsRef(stateMachine)));
        }

        public void Dispose() => _stateMachine.Dispose();
        void IObserver<TSource>.OnNext(TSource value) => _stateMachine.OnNext(value);
        void IObserver<TSource>.OnError(Exception error) => _stateMachine.OnError(error);
        void IObserver<TSource>.OnCompleted() => _stateMachine.OnCompleted();
    }

    internal struct ObservableStateMachine<T, TStateMachine> : IObserverStateMachine<T>
    {
        private Observable<T> _observable;

        public void Initialize(IObserverStateMachineBox box) => _observable = (Observable<T>) box;
        public void Dispose() { }

        public void OnNext(T value) => _observable.OnNext(value);
        public void OnError(Exception error) => _observable.OnError(error);
        public void OnCompleted() => _observable.OnCompleted();
    }

    internal struct ObservableFactory<TResult> : IObserverFactory<Observable<TResult>>
    {
        public Observable<TResult> Create<TSource, TStateMachine>(in TStateMachine stateMachine)
            where TStateMachine : struct, IObserverStateMachine<TSource>
        {
            return new Observable<TResult, TSource, TStateMachine>(stateMachine);
        }
    }
}