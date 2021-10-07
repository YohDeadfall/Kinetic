using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Kinetic.Linq.StateMachines
{
    internal abstract class ObservableStateMachineBox<T> : Observable<T>
    {
        public new void OnNext(T value) => base.OnNext(value);
        public new void OnError(Exception error) => base.OnError(error);
        public new void OnCompleted() => base.OnCompleted();
    }

    internal sealed class ObservableStateMachineBox<TResult, TSource, TStateMachine> : ObservableStateMachineBox<TResult>, IObserver<TSource>, IObserverStateMachineBox, IDisposable
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
}