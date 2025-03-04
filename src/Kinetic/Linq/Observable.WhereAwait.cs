using System;
using System.Threading.Tasks;
using Kinetic.Linq.StateMachines;

namespace Kinetic.Linq;

public static partial class Observable
{
    public static ObserverBuilder<TSource> WhereAwait<TSource>(this ObserverBuilder<TSource> source, Func<TSource, Task<bool>> predicate) =>
        source.ContinueWith<WhereAwaitStateMachineFactory<TSource>, TSource>(new(predicate));

    public static ObserverBuilder<TSource> WhereAwait<TSource>(this IObservable<TSource> source, Func<TSource, Task<bool>> predicate) =>
        source.ToBuilder().WhereAwait(predicate);

    public static ObserverBuilder<TSource> WhereAwait<TSource>(this ObserverBuilder<TSource> source, Func<TSource, ValueTask<bool>> predicate) =>
        source.ContinueWith<WhereAwaitStateMachineFactory<TSource>, TSource>(new(predicate));

    public static ObserverBuilder<TSource> WhereAwait<TSource>(this IObservable<TSource> source, Func<TSource, ValueTask<bool>> predicate) =>
        source.ToBuilder().WhereAwait(predicate);

    private readonly struct WhereAwaitStateMachineFactory<TSource> : IStateMachineFactory<TSource, TSource>
    {
        private readonly Delegate _predicate;

        public WhereAwaitStateMachineFactory(Func<TSource, Task<bool>> predicate) =>
            _predicate = predicate;

        public WhereAwaitStateMachineFactory(Func<TSource, ValueTask<bool>> predicate) =>
            _predicate = predicate;

        public void Create<TContinuation>(in TContinuation continuation, ObserverStateMachine<TSource> source)
            where TContinuation : struct, IStateMachine<TSource>
        {
            if (_predicate is Func<TSource, Task<bool>> taskPredicate)
            {
                source.ContinueWith(
                    new WhereAwaitStateMachine<
                        TSource,
                        TContinuation,
                        AwaiterForTask<bool>,
                        AwaiterFactoryForTask<TSource, bool>>
                        (continuation, new(taskPredicate)));

                return;
            }

            if (_predicate is Func<TSource, ValueTask<bool>> valueTaskPredicate)
            {
                source.ContinueWith(
                    new WhereAwaitStateMachine<
                        TSource,
                        TContinuation,
                        AwaiterForValueTask<bool>,
                        AwaiterFactoryForValueTask<TSource, bool>>
                        (continuation, new(valueTaskPredicate)));

                return;
            }

            throw new NotSupportedException();
        }
    }

    private struct WhereAwaitStateMachine<TSource, TContinuation, TAwaiter, TAwaiterFactory> : IStateMachine<TSource>
        where TContinuation : struct, IStateMachine<TSource>
        where TAwaiter : struct, IAwaiter<bool>
        where TAwaiterFactory : struct, IAwaiterFactory<TAwaiter, TSource, bool>
    {
        private TContinuation _continuation;
        private readonly TAwaiterFactory _predicate;

        public WhereAwaitStateMachine(in TContinuation continuation, TAwaiterFactory predicate)
        {
            _continuation = continuation;
            _predicate = predicate;
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
                awaiter = _predicate.GetAwaiter(value);
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
            var result = false;
            try
            {
                result = awaiter.GetResult();
            }
            catch (Exception error)
            {
                continuation.OnError(error);
                return;
            }

            if (result)
            {
                continuation.OnNext(value);
            }
        }
    }
}