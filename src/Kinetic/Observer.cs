using System;
using System.Runtime.CompilerServices;

namespace Kinetic
{
    public interface IObserverStateMachine<T> : IObserver<T>, IDisposable
    {
        void Initialize(IObserverStateMachineBox box);
    }

    public interface IObserverStateMachineBox
    {
        IDisposable Subscribe<T, TStateMachine>(IObservable<T> observable, ref TStateMachine stateMachine)
            where TStateMachine : struct, IObserverStateMachine<T>;
    }

    public interface IObserverFactory
    {
        void Create<T, TStateMachine>(in TStateMachine stateMachine)
            where TStateMachine : struct, IObserverStateMachine<T>;
    }

    public interface IObserverBuilder<T>
    {
        void Build<TStateMachine, TFactory>(in TStateMachine stateMachine, ref TFactory factory)
            where TStateMachine : struct, IObserverStateMachine<T>
            where TFactory : struct, IObserverFactory;
    }

    public readonly struct ObserverBuilder<T> : IObserverBuilder<T>
    {
        private readonly IObservable<T> _observable;

        public ObserverBuilder(IObservable<T> observable) =>
            _observable = observable;

        public void Build<TStateMachine, TFactory>(in TStateMachine stateMachine, ref TFactory factory)
            where TStateMachine : struct, IObserverStateMachine<T>
            where TFactory : struct, IObserverFactory
        {
            var subscribe = new Subscribe<TStateMachine>(stateMachine, _observable);
            factory.Create<T, Subscribe<TStateMachine>>(subscribe);
        }

        private struct Subscribe<TStateMachine> : IObserverStateMachine<T>
            where TStateMachine : struct, IObserverStateMachine<T>
        {
            private TStateMachine _stateMachine;
            private IObservable<T> _observable;
            private IDisposable? _subscription;

            public Subscribe(in TStateMachine stateMachine, IObservable<T> observable)
            {
                _stateMachine = stateMachine;
                _observable = observable;
                _subscription = null;
            }

            public void OnNext(T value) => _stateMachine.OnNext(value);
            public void OnError(Exception error) => _stateMachine.OnError(error);
            public void OnCompleted() => _stateMachine.OnCompleted();

            public void Initialize(IObserverStateMachineBox box) =>
                _subscription = _observable.Subscribe((Observer<T>) box);

            public void Dispose() =>
                _subscription?.Dispose();
        }
    }

    public static class ObserverBuilder
    {
        public static ObserverBuilder<T> ToBuilder<T>(this IObservable<T> observable) => new(observable);
    }

    internal abstract class Observer<T> : IObserver<T>, IDisposable
    {
        public abstract void Dispose();
        public abstract void OnNext(T value);
        public abstract void OnError(Exception error);
        public abstract void OnCompleted();
    }

    internal sealed class Observer<TSource, TStateMachine> : Observer<TSource>, IObserverStateMachineBox
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

        public IDisposable Subscribe<T, TStateMachinePart>(IObservable<T> observable, ref TStateMachinePart stateMachine)
            where TStateMachinePart : struct, IObserverStateMachine<T>
        {
            return observable.Subscribe(
                state: (self: this, offset: GetStateMachineOffset(ref stateMachine)),
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
            return ref stateMachinePart;
        }

        private IntPtr GetStateMachineOffset<TStateMachinePart>(ref TStateMachinePart stateMachine)
        {
            return Unsafe.ByteOffset(
                ref Unsafe.As<TStateMachine, IntPtr>(ref _stateMachine),
                ref Unsafe.As<TStateMachinePart, IntPtr>(ref stateMachine));
        }

        public override void Dispose() => _stateMachine.Dispose();
        public override void OnNext(TSource value) => _stateMachine.OnNext(value);
        public override void OnError(Exception error) => _stateMachine.OnError(error);
        public override void OnCompleted() => _stateMachine.OnCompleted();
    }
}