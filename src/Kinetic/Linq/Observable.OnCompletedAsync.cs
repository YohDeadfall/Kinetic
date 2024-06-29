using System;
using System.Threading.Tasks;
using Kinetic.Linq.StateMachines;

namespace Kinetic.Linq;

public static partial class Observable
{
    public static ObserverBuilder<TSource> OnCompletedAsync<TSource>(this IObservable<TSource> source, Func<Task> onCompleted) =>
        source.ToBuilder().OnCompletedAsync(onCompleted);

    public static ObserverBuilder<TSource> OnError<TSource>(this IObservable<TSource> source, Func<ValueTask> onCompleted) =>
        source.ToBuilder().OnCompletedAsync(onCompleted);

    public static ObserverBuilder<TSource> OnCompletedAsync<TSource>(this ObserverBuilder<TSource> source, Func<Task> onCompleted) =>
        source.ContinueWith<OnCompletedAsyncStateMachineFactory<TSource>, TSource>(new(onCompleted));

    public static ObserverBuilder<TSource> OnCompletedAsync<TSource>(this ObserverBuilder<TSource> source, Func<ValueTask> onCompleted) =>
        source.ContinueWith<OnCompletedAsyncStateMachineFactory<TSource>, TSource>(new(onCompleted));

    private readonly struct OnCompletedAsyncStateMachineFactory<TSource> : IStateMachineFactory<TSource, TSource>
    {
        private readonly Delegate _onError;

        public OnCompletedAsyncStateMachineFactory(Func<Task> onError) =>
            _onError = onError;

        public OnCompletedAsyncStateMachineFactory(Func<ValueTask> onError) =>
            _onError = onError;

        public void Create<TContinuation>(in TContinuation continuation, ObserverStateMachine<TSource> source)
            where TContinuation : struct, IStateMachine<TSource>
        {
            if (_onError is Func<Task> onCompleted)
            {
                source.ContinueWith<OnCompletedAsyncStateMachine<TSource, TContinuation, AwaiterForTask, AwaiterFactoryForTask>>(
                    new(continuation, new(onCompleted)));

                return;
            }

            if (_onError is Func<ValueTask> onCopletedValueTask)
            {
                source.ContinueWith<OnCompletedAsyncStateMachine<TSource, TContinuation, AwaiterForValueTask, AwaiterFactoryForValueTask>>(
                    new(continuation, new(onCopletedValueTask)));

                return;
            }

            throw new NotSupportedException();
        }
    }

    private struct OnCompletedAsyncStateMachine<TSource, TContinuation, TAwaiter, TAwaiterFactory> : IStateMachine<TSource>
        where TContinuation : struct, IStateMachine<TSource>
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
                var reference = new StateMachineReference<TSource, TContinuation>(ref _continuation);
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