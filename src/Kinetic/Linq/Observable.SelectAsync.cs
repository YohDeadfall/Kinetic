using System;
using System.Threading.Tasks;
using Kinetic.Linq.StateMachines;

namespace Kinetic.Linq;

public static partial class Observable
{
    public static ObserverBuilder<TResult> SelectAsync<TSource, TResult>(this ObserverBuilder<TSource> source, Func<TSource, Task<TResult>> selector) =>
        source.ContinueWith<SelectAsyncStateMachineFactory<TSource, TResult>, TResult>(new(selector));

    public static ObserverBuilder<TResult> SelectAsync<TSource, TResult>(this IObservable<TSource> source, Func<TSource, Task<TResult>> selector) =>
        source.ToBuilder().SelectAsync(selector);

    public static ObserverBuilder<TResult> SelectAsync<TSource, TResult>(this ObserverBuilder<TSource> source, Func<TSource, ValueTask<TResult>> selector) =>
        source.ContinueWith<SelectAsyncStateMachineFactory<TSource, TResult>, TResult>(new(selector));

    public static ObserverBuilder<TResult> SelectAsync<TSource, TResult>(this IObservable<TSource> source, Func<TSource, ValueTask<TResult>> selector) =>
        source.ToBuilder().SelectAsync(selector);

    private readonly struct SelectAsyncStateMachineFactory<TSource, TResult> : IStateMachineFactory<TSource, TResult>
    {
        private readonly Delegate _selector;

        public SelectAsyncStateMachineFactory(Func<TSource, Task<TResult>> selector) =>
            _selector = selector;

        public SelectAsyncStateMachineFactory(Func<TSource, ValueTask<TResult>> selector) =>
            _selector = selector;

        public void Create<TContinuation>(in TContinuation continuation, ObserverStateMachine<TSource> source)
            where TContinuation : struct, IStateMachine<TResult>
        {
            if (_selector is Func<TSource, Task<TResult>> taskSelector)
            {
                source.ContinueWith(
                    new SelectAsyncStateMachine<
                        TSource,
                        TResult,
                        TContinuation,
                        AwaiterForTask<TResult>,
                        AwaiterFactoryForTask<TSource, TResult>>
                        (continuation, new(taskSelector)));

                return;
            }

            if (_selector is Func<TSource, ValueTask<TResult>> valueTaskSelector)
            {
                source.ContinueWith(
                    new SelectAsyncStateMachine<
                        TSource,
                        TResult,
                        TContinuation,
                        AwaiterForValueTask<TResult>,
                        AwaiterFactoryForValueTask<TSource, TResult>>
                        (continuation, new(valueTaskSelector)));

                return;
            }

            throw new NotSupportedException();
        }
    }

    private struct SelectAsyncStateMachine<TSource, TResult, TContinuation, TAwaiter, TAwaiterFactory> : IStateMachine<TSource>
        where TContinuation : struct, IStateMachine<TResult>
        where TAwaiter : struct, IAwaiter<TResult>
        where TAwaiterFactory : struct, IAwaiterFactory<TAwaiter, TSource, TResult>
    {
        private TContinuation _continuation;
        private readonly TAwaiterFactory _selector;

        public SelectAsyncStateMachine(in TContinuation continuation, TAwaiterFactory selector)
        {
            _continuation = continuation;
            _selector = selector;
        }

        public StateMachineBox Box =>
            _continuation.Box;

        public void Initialize(StateMachineBox box) =>
            _continuation.Initialize(box);

        public void Dispose() =>
            _continuation.Dispose();

        public void OnNext(TSource value)
        {
            var awaiter = default(TAwaiter);
            try
            {
                awaiter = _selector.GetAwaiter(value);
            }
            catch (Exception error)
            {
                _continuation.OnError(error);
                return;
            }

            if (awaiter.IsCompleted)
            {
                ForwardResult(awaiter, ref _continuation);
            }
            else
            {
                var reference = new StateMachineReference<TResult, TContinuation>(ref _continuation);
                var completion = () => ForwardResult(awaiter, ref reference.Target);

                awaiter.OnCompleted(completion);
            }
        }

        public void OnError(Exception error) =>
            _continuation.OnError(error);

        public void OnCompleted() =>
            _continuation.OnCompleted();

        private static void ForwardResult(TAwaiter awaiter, ref TContinuation continuation)
        {
            var result = default(TResult);
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception error)
            {
                continuation.OnError(error);
                return;
            }

            continuation.OnNext(result);
        }
    }
}