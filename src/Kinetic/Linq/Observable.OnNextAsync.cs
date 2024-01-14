using System;
using System.Threading.Tasks;
using Kinetic.Linq.StateMachines;

namespace Kinetic.Linq;

public static partial class Observable
{
    public static ObserverBuilder<T> OnNextAsync<T>(this IObservable<T> source, Func<T, Task> onNext) =>
        source.ToBuilder().OnNextAsync(onNext);

    public static ObserverBuilder<T> OnNextAsync<T>(this IObservable<T> source, Func<T, ValueTask> onNext) =>
        source.ToBuilder().OnNextAsync(onNext);

    public static ObserverBuilder<T> OnNextAsync<T>(this ObserverBuilder<T> source, Func<T, Task> onNext) =>
        source.ContinueWith<OnNextAsyncStateMachineFactory<T>, T>(new(onNext));

    public static ObserverBuilder<T> OnNextAsync<T>(this ObserverBuilder<T> source, Func<T, ValueTask> onNext) =>
        source.ContinueWith<OnNextAsyncStateMachineFactory<T>, T>(new(onNext));

    private readonly struct OnNextAsyncStateMachineFactory<T> : IStateMachineFactory<T, T>
    {
        private readonly Delegate _onNext;

        public OnNextAsyncStateMachineFactory(Func<T, Task> onNext) =>
            _onNext = onNext;

        public OnNextAsyncStateMachineFactory(Func<T, ValueTask> onNext) =>
            _onNext = onNext;

        public void Create<TContinuation>(in TContinuation continuation, ObserverStateMachine<T> source)
            where TContinuation : struct, IStateMachine<T>
        {
            if (_onNext is Func<T, Task> onNextTask)
            {
                source.ContinueWith(new OnNextAsyncStateMachine<TContinuation, T, AwaiterForTask, AwaiterFactoryForTask<T>>(continuation, new(onNextTask)));
                return;
            }

            if (_onNext is Func<T, ValueTask> onNextValueTask)
            {
                source.ContinueWith(new OnNextAsyncStateMachine<TContinuation, T, AwaiterForValueTask, AwaiterFactoryForValueTask<T>>(continuation, new(onNextValueTask)));
                return;
            }

            throw new NotSupportedException();
        }
    }

    private struct OnNextAsyncStateMachine<TContinuation, T, TAwaiter, TAwaiterFactory> : IStateMachine<T>
        where TContinuation : struct, IStateMachine<T>
        where TAwaiter : struct, IAwaiter
        where TAwaiterFactory : struct, IAwaiterFactory<TAwaiter, T>
    {
        private TContinuation _continuation;
        private readonly TAwaiterFactory _onNext;

        public OnNextAsyncStateMachine(in TContinuation continuation, TAwaiterFactory onNext)
        {
            _continuation = continuation;
            _onNext = onNext;
        }

        public StateMachineBox Box =>
            _continuation.Box;

        public void Initialize(StateMachineBox box) =>
            _continuation.Initialize(box);

        public void Dispose() =>
            _continuation.Dispose();

        public void OnNext(T value)
        {
            var awaiter = default(TAwaiter);
            try
            {
                awaiter = _onNext.GetAwaiter(value);
            }
            catch (Exception error)
            {
                _continuation.OnError(error);
                return;
            }

            if (awaiter.IsCompleted)
            {
                ForwardResult(value, awaiter, ref _continuation);
            }
            else
            {
                var reference = new StateMachineReference<T, TContinuation>(ref _continuation);
                var completion = () => ForwardResult(value, awaiter, ref reference.Target);

                awaiter.OnCompleted(completion);
            }
        }

        public void OnError(Exception error) =>
            _continuation.OnError(error);

        public void OnCompleted() =>
            _continuation.OnCompleted();

        private static void ForwardResult(T value, TAwaiter awaiter, ref TContinuation continuation)
        {
            try
            {
                awaiter.GetResult();
            }
            catch (Exception error)
            {
                continuation.OnError(error);
                return;
            }

            continuation.OnNext(value);
        }
    }
}