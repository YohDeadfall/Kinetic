using System;
using System.Threading.Tasks;
using Kinetic.Linq.StateMachines;

namespace Kinetic.Linq;

public static partial class Observable
{
    public static ObserverBuilder<TSource> OnErrorAsync<TSource>(this IObservable<TSource> source, Func<Exception, Task> onError) =>
        source.ToBuilder().OnErrorAsync(onError);

    public static ObserverBuilder<TSource> OnErrorAsync<TSource>(this IObservable<TSource> source, Func<Exception, ValueTask> onError) =>
        source.ToBuilder().OnErrorAsync(onError);

    public static ObserverBuilder<TSource> OnErrorAsync<TSource>(this ObserverBuilder<TSource> source, Func<Exception, Task> onError) =>
        source.ContinueWith<OnErrorAsyncStateMachineFactory<TSource>, TSource>(new(onError));

    public static ObserverBuilder<TSource> OnErrorAsync<TSource>(this ObserverBuilder<TSource> source, Func<Exception, ValueTask> onError) =>
        source.ContinueWith<OnErrorAsyncStateMachineFactory<TSource>, TSource>(new(onError));

    private readonly struct OnErrorAsyncStateMachineFactory<TSource> : IStateMachineFactory<TSource, TSource>
    {
        private readonly Delegate _onError;

        public OnErrorAsyncStateMachineFactory(Func<Exception, Task> onError) =>
            _onError = onError;

        public OnErrorAsyncStateMachineFactory(Func<Exception, ValueTask> onError) =>
            _onError = onError;

        public void Create<TContinuation>(in TContinuation continuation, ObserverStateMachine<TSource> source)
            where TContinuation : struct, IStateMachine<TSource>
        {
            if (_onError is Func<Exception, Task> onErrorTask)
            {
                source.ContinueWith<OnErrorAsyncStateMachine<TSource, TContinuation, AwaiterForTask, AwaiterFactoryForTask<Exception>>>(
                    new(continuation, new(onErrorTask)));
                return;
            }

            if (_onError is Func<Exception, ValueTask> onErrorValueTask)
            {
                source.ContinueWith<OnErrorAsyncStateMachine<TSource, TContinuation, AwaiterForValueTask, AwaiterFactoryForValueTask<Exception>>>(
                    new(continuation, new(onErrorValueTask)));

                return;
            }

            throw new NotSupportedException();
        }
    }

    private struct OnErrorAsyncStateMachine<TSource, TContinuation, TAwaiter, TAwaiterFactory> : IStateMachine<TSource>
        where TContinuation : struct, IStateMachine<TSource>
        where TAwaiter : struct, IAwaiter
        where TAwaiterFactory : struct, IAwaiterFactory<TAwaiter, Exception>
    {
        private TContinuation _continuation;
        private readonly TAwaiterFactory _onError;

        public OnErrorAsyncStateMachine(in TContinuation continuation, TAwaiterFactory onError)
        {
            _continuation = continuation;
            _onError = onError;
        }

        public StateMachineBox Box =>
            _continuation.Box;

        public StateMachine<TSource> Reference =>
            StateMachine<TSource>.Create(ref this);

        public StateMachine? Continuation =>
            _continuation.Reference;

        public void Initialize(StateMachineBox box) =>
            _continuation.Initialize(box);

        public void Dispose() =>
            _continuation.Dispose();

        public void OnNext(TSource value) =>
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
                var reference = new StateMachineReference<TSource, TContinuation>(ref _continuation);
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