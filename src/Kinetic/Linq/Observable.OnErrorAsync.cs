using System;
using System.Threading.Tasks;
using Kinetic.Linq.StateMachines;

namespace Kinetic.Linq;

public static partial class Observable
{
    public static ObserverBuilder<T> OnErrorAsync<T>(this IObservable<T> source, Func<Exception, Task> onError) =>
        source.ToBuilder().OnErrorAsync(onError);

    public static ObserverBuilder<T> OnErrorAsync<T>(this IObservable<T> source, Func<Exception, ValueTask> onError) =>
        source.ToBuilder().OnErrorAsync(onError);

    public static ObserverBuilder<T> OnErrorAsync<T>(this ObserverBuilder<T> source, Func<Exception, Task> onError) =>
        source.ContinueWith<OnErrorAsyncStateMachineFactory<T>, T>(new(onError));

    public static ObserverBuilder<T> OnErrorAsync<T>(this ObserverBuilder<T> source, Func<Exception, ValueTask> onError) =>
        source.ContinueWith<OnErrorAsyncStateMachineFactory<T>, T>(new(onError));

    private readonly struct OnErrorAsyncStateMachineFactory<T> : IObserverStateMachineFactory<T, T>
    {
        private readonly Delegate _onError;

        public OnErrorAsyncStateMachineFactory(Func<Exception, Task> onError) =>
            _onError = onError;

        public OnErrorAsyncStateMachineFactory(Func<Exception, ValueTask> onError) =>
            _onError = onError;

        public void Create<TContinuation>(in TContinuation continuation, ObserverStateMachine<T> source)
            where TContinuation : struct, IObserverStateMachine<T>
        {
            if (_onError is Func<Exception, Task> onErrorTask)
            {
                source.ContinueWith(new OnErrorAsyncStateMachine<TContinuation, T, AwaiterForTask, AwaiterFactoryForTask<Exception>>(continuation, new(onErrorTask)));
                return;
            }

            if (_onError is Func<Exception, ValueTask> onErrorValueTask)
            {
                source.ContinueWith(new OnErrorAsyncStateMachine<TContinuation, T, AwaiterForValueTask, AwaiterFactoryForValueTask<Exception>>(continuation, new(onErrorValueTask)));

                return;
            }

            throw new NotSupportedException();
        }
    }

    private struct OnErrorAsyncStateMachine<TContinuation, T, TAwaiter, TAwaiterFactory> : IObserverStateMachine<T>
        where TContinuation : struct, IObserverStateMachine<T>
        where TAwaiter : struct, IAwaiter
        where TAwaiterFactory : struct, IAwaiterFactory<TAwaiter, Exception>
    {
        private TContinuation _continuation;
        private readonly TAwaiterFactory _onError;

        public OnErrorAsyncStateMachine(in TContinuation continuation, TAwaiterFactory onNext)
        {
            _continuation = continuation;
            _onError = onNext;
        }

        public ObserverStateMachineBox Box =>
            _continuation.Box;

        public void Initialize(ObserverStateMachineBox box) =>
            _continuation.Initialize(box);

        public void Dispose() =>
            _continuation.Dispose();

        public void OnNext(T value) =>
            _continuation.OnNext(value);

        public void OnError(Exception error)
        {
            var awaiter = default(TAwaiter);
            try
            {
                awaiter = _onError.GetAwaiter(error);
            }
            catch (Exception errorOnceAgain)
            {
                _continuation.OnError(errorOnceAgain);
                return;
            }

            if (awaiter.IsCompleted)
            {
                ForwardResult(error, awaiter, ref _continuation);
            }
            else
            {
                var reference = new ObserverStateMachineReference<T, TContinuation>(ref _continuation);
                var completion = () => ForwardResult(error, awaiter, ref reference.Target);

                awaiter.OnCompleted(completion);
            }
        }

        public void OnCompleted() =>
            _continuation.OnCompleted();

        private static void ForwardResult(Exception error, TAwaiter awaiter, ref TContinuation continuation)
        {
            try
            {
                awaiter.GetResult();
            }
            catch (Exception errorOnceAgain)
            {
                error = errorOnceAgain;
                return;
            }

            continuation.OnError(error);
        }
    }
}