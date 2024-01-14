using System;
using System.Threading.Tasks;
using Kinetic.Linq.StateMachines;

namespace Kinetic.Linq;

public static partial class Observable
{
    public static ObserverBuilder<T> OnCompletedAsync<T>(this IObservable<T> source, Func<Task> onCompleted) =>
        source.ToBuilder().OnCompletedAsync(onCompleted);

    public static ObserverBuilder<T> OnError<T>(this IObservable<T> source, Func<ValueTask> onCompleted) =>
        source.ToBuilder().OnCompletedAsync(onCompleted);

    public static ObserverBuilder<T> OnCompletedAsync<T>(this ObserverBuilder<T> source, Func<Task> onCompleted) =>
        source.ContinueWith<OnCompletedAsyncStateMachineFactory<T>, T>(new(onCompleted));

    public static ObserverBuilder<T> OnCompletedAsync<T>(this ObserverBuilder<T> source, Func<ValueTask> onCompleted) =>
        source.ContinueWith<OnCompletedAsyncStateMachineFactory<T>, T>(new(onCompleted));

    private readonly struct OnCompletedAsyncStateMachineFactory<T> : IStateMachineFactory<T, T>
    {
        private readonly Delegate _onError;

        public OnCompletedAsyncStateMachineFactory(Func<Task> onError) =>
            _onError = onError;

        public OnCompletedAsyncStateMachineFactory(Func<ValueTask> onError) =>
            _onError = onError;

        public void Create<TContinuation>(in TContinuation continuation, ObserverStateMachine<T> source)
            where TContinuation : struct, IStateMachine<T>
        {
            if (_onError is Func<Task> onCompleted)
            {
                source.ContinueWith(new OnCompletedAsyncStateMachine<TContinuation, T, AwaiterForTask, AwaiterFactoryForTask>(continuation, new(onCompleted)));

                return;
            }

            if (_onError is Func<ValueTask> onCopletedValueTask)
            {
                source.ContinueWith(new OnCompletedAsyncStateMachine<TContinuation, T, AwaiterForValueTask, AwaiterFactoryForValueTask>(continuation, new(onCopletedValueTask)));

                return;
            }

            throw new NotSupportedException();
        }
    }

    private struct OnCompletedAsyncStateMachine<TContinuation, T, TAwaiter, TAwaiterFactory> : IStateMachine<T>
        where TContinuation : struct, IStateMachine<T>
        where TAwaiter : struct, IAwaiter
        where TAwaiterFactory : struct, IAwaiterFactory<TAwaiter>
    {
        private TContinuation _continuation;
        private readonly TAwaiterFactory _onCompleted;

        public OnCompletedAsyncStateMachine(in TContinuation continuation, TAwaiterFactory onCompleted)
        {
            _continuation = continuation;
            _onCompleted = onCompleted;
        }

        public StateMachineBox Box =>
            _continuation.Box;

        public void Initialize(StateMachineBox box) =>
            _continuation.Initialize(box);

        public void Dispose() =>
            _continuation.Dispose();

        public void OnNext(T value) =>
            _continuation.OnNext(value);

        public void OnError(Exception error) =>
            _continuation.OnError(error);

        public void OnCompleted()
        {
            var awaiter = default(TAwaiter);
            try
            {
                awaiter = _onCompleted.GetAwaiter();
            }
            catch (Exception errorOnceAgain)
            {
                _continuation.OnError(errorOnceAgain);
                return;
            }

            if (awaiter.IsCompleted)
            {
                ForwardResult(awaiter, ref _continuation);
            }
            else
            {
                var reference = new StateMachineReference<T, TContinuation>(ref _continuation);
                var completion = () => ForwardResult(awaiter, ref reference.Target);

                awaiter.OnCompleted(completion);
            }
        }

        private static void ForwardResult(TAwaiter awaiter, ref TContinuation continuation)
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

            continuation.OnCompleted();
        }
    }
}