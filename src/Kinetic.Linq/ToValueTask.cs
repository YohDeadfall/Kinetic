using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using Kinetic.Linq.StateMachines;

namespace Kinetic.Linq
{
    public static partial class Observable
    {
        public static ValueTask<TResult> ToValueTask<TResult>(this IObservable<TResult> source) =>
            source.ToBuilder().ToValueTask();

        public static ValueTask<TResult> ToValueTask<TResult>(this in ObserverBuilder<TResult> source)
        {
            var taskSource = source.Build<ValueTaskSourceStateMachine<TResult>, ValueTaskSourceFactory<TResult>, ValueTaskSource<TResult>>(
                continuation: new(),
                factory: new());

            return new ValueTask<TResult>(taskSource, taskSource.Token);
        }

        private struct ValueTaskSourceStateMachine<TResult> : IObserverStateMachine<TResult>
        {
            private ValueTaskSource<TResult> _taskSource;

            public void Initialize(IObserverStateMachineBox box) => _taskSource = (ValueTaskSource<TResult>) box;
            public void Dispose() { }

            public void OnNext(TResult value) => _taskSource.OnNext(value);
            public void OnError(Exception error) => _taskSource.OnError(error);
            public void OnCompleted() { }
        }

        private abstract class ValueTaskSource<TResult> : IValueTaskSource<TResult>
        {
            private ManualResetValueTaskSourceCore<TResult> _core;

            public short Token => _core.Version;

            public ValueTaskSourceStatus GetStatus(short token) => _core.GetStatus(token);
            public TResult GetResult(short token) => _core.GetResult(token);

            public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags) =>
                _core.OnCompleted(continuation, state, token, flags);

            public void OnNext(TResult value) => _core.SetResult(value);
            public void OnError(Exception error) => _core.SetException(error);
        }

        private sealed class ValueTaskSource<TResult, TSource, TStateMachine> : ValueTaskSource<TResult>, IObserver<TSource>, IObserverStateMachineBox, IDisposable
            where TStateMachine : struct, IObserverStateMachine<TSource>
        {
            private TStateMachine _stateMachine;

            public ValueTaskSource(in TStateMachine stateMachine)
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

        private struct ValueTaskSourceFactory<TResult> : IObserverFactory<ValueTaskSource<TResult>>
        {
            public ValueTaskSource<TResult> Create<TSource, TStateMachine>(in TStateMachine stateMachine)
                where TStateMachine : struct, IObserverStateMachine<TSource>
            {
                return new ValueTaskSource<TResult, TSource, TStateMachine>(stateMachine);
            }
        }
    }
}