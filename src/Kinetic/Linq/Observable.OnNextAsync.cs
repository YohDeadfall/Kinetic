using System;
using System.Threading.Tasks;
using Kinetic.Linq.StateMachines;

namespace Kinetic.Linq;

public static partial class Observable
{
    public static ObserverBuilder<TSource> OnNextAsync<TSource>(this IObservable<TSource> source, Func<TSource, Task> onNext) =>
        source.ToBuilder().OnNextAsync(onNext);

    public static ObserverBuilder<TSource> OnNextAsync<TSource>(this IObservable<TSource> source, Func<TSource, ValueTask> onNext) =>
        source.ToBuilder().OnNextAsync(onNext);

    public static ObserverBuilder<TSource> OnNextAsync<TSource>(this ObserverBuilder<TSource> source, Func<TSource, Task> onNext) =>
        source.ContinueWith<OnNextAsyncStateMachineFactory<TSource>, TSource>(new(onNext));

    public static ObserverBuilder<TSource> OnNextAsync<TSource>(this ObserverBuilder<TSource> source, Func<TSource, ValueTask> onNext) =>
        source.ContinueWith<OnNextAsyncStateMachineFactory<TSource>, TSource>(new(onNext));

    private readonly struct OnNextAsyncStateMachineFactory<TSource> : IStateMachineFactory<TSource, TSource>
    {
        private readonly Delegate _onNext;

        public OnNextAsyncStateMachineFactory(Func<TSource, Task> onNext) =>
            _onNext = onNext;

        public OnNextAsyncStateMachineFactory(Func<TSource, ValueTask> onNext) =>
            _onNext = onNext;

        public void Create<TContinuation>(in TContinuation continuation, ObserverStateMachine<TSource> source)
            where TContinuation : struct, IStateMachine<TSource>
        {
            if (_onNext is Func<TSource, Task> onNextTask)
            {
                source.ContinueWith<OnNextAsyncStateMachine<TSource, TContinuation, AwaiterForTask, AwaiterFactoryForTask<TSource>>>(
                    new(continuation, new(onNextTask)));
                return;
            }

            if (_onNext is Func<TSource, ValueTask> onNextValueTask)
            {
                source.ContinueWith<OnNextAsyncStateMachine<TSource, TContinuation, AwaiterForValueTask, AwaiterFactoryForValueTask<TSource>>>(
                    new(continuation, new(onNextValueTask)));
                return;
            }

            throw new NotSupportedException();
        }
    }

    private struct OnNextAsyncStateMachine<TSource, TContinuation, TAwaiter, TAwaiterFactory> : IStateMachine<TSource>
        where TContinuation : struct, IStateMachine<TSource>
        where TAwaiter : struct, IAwaiter
        where TAwaiterFactory : struct, IAwaiterFactory<TAwaiter, TSource>
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

        public StateMachine<TSource> Reference =>
            StateMachine<TSource>.Create(ref this);

        public StateMachine? Continuation =>
            _continuation.Reference;

        public void Initialize(StateMachineBox box) =>
            _continuation.Initialize(box);

        public void Dispose() =>
            _continuation.Dispose();

        public void OnNext(TSource value)
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
                var reference = new StateMachineReference<TSource, TContinuation>(ref _continuation);
                var completion = () => ForwardResult(value, awaiter, ref reference.Target);

                awaiter.OnCompleted(completion);
            }
        }

        public void OnError(Exception error) =>
            _continuation.OnError(error);

        public void OnCompleted() =>
            _continuation.OnCompleted();

        private static void ForwardResult(TSource value, TAwaiter awaiter, ref TContinuation continuation)
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