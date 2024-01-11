using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Kinetic.Linq.StateMachines;

namespace Kinetic.Linq;

public static partial class Observable
{
    public static ObserverBuilder<TSource> WhereAsync<TSource>(this ObserverBuilder<TSource> source, Func<TSource, Task<bool>> predicate) =>
        source.ContinueWith<WhereAsyncStateMachineFactory<TSource>, TSource>(new(predicate));

    public static ObserverBuilder<TSource> WhereAsync<TSource>(this IObservable<TSource> source, Func<TSource, Task<bool>> predicate) =>
        source.ToBuilder().WhereAsync(predicate);

    public static ObserverBuilder<TSource> WhereAsync<TSource>(this ObserverBuilder<TSource> source, Func<TSource, ValueTask<bool>> predicate) =>
        source.ContinueWith<WhereAsyncStateMachineFactory<TSource>, TSource>(new(predicate));

    public static ObserverBuilder<TSource> WhereAsync<TSource>(this IObservable<TSource> source, Func<TSource, ValueTask<bool>> predicate) =>
        source.ToBuilder().WhereAsync(predicate);

    private readonly struct WhereAsyncStateMachineFactory<TSource> : IObserverStateMachineFactory<TSource, TSource>
    {
        private readonly Delegate _predicate;

        public WhereAsyncStateMachineFactory(Func<TSource, Task<bool>> predicate) =>
            _predicate = predicate;

        public WhereAsyncStateMachineFactory(Func<TSource, ValueTask<bool>> predicate) =>
            _predicate = predicate;

        public void Create<TContinuation>(in TContinuation continuation, ObserverStateMachine<TSource> source)
            where TContinuation : struct, IObserverStateMachine<TSource>
        {
            if (_predicate is Func<TSource, Task<bool>> taskPredicate)
            {
                source.ContinueWith(
                    new WhereAsyncStateMachine<
                        TContinuation,
                        TSource,
                        AwaiterForTask<bool>,
                        AwaiterFactoryForTask<TSource, bool>>
                        (continuation, new(taskPredicate)));

                return;
            }

            if (_predicate is Func<TSource, ValueTask<bool>> valueTaskPredicate)
            {
                source.ContinueWith(
                    new WhereAsyncStateMachine<
                        TContinuation,
                        TSource,
                        AwaiterForValueTask<bool>,
                        AwaiterFactoryForValueTask<TSource, bool>>
                        (continuation, new(valueTaskPredicate)));

                return;
            }

            throw new NotSupportedException();
        }
    }

    private struct WhereAsyncStateMachine<TContinuation, TSource, TAwaiter, TAwaiterFactory> : IObserverStateMachine<TSource>
        where TContinuation : struct, IObserverStateMachine<TSource>
        where TAwaiter : struct, IAwaiter<bool>
        where TAwaiterFactory : struct, IAwaiterFactory<TAwaiter, TSource, bool>
    {
        private TContinuation _continuation;
        private ObserverStateMachineBox? _box;
        private readonly TAwaiterFactory _predicate;

        public WhereAsyncStateMachine(in TContinuation continuation, TAwaiterFactory predicate)
        {
            _continuation = continuation;
            _predicate = predicate;
        }

        public void Initialize(ObserverStateMachineBox box)
        {
            _box = box;
            _continuation.Initialize(box);
        }

        public void Dispose() => _continuation.Dispose();

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
                Debug.Assert(_box is not null);

                var reference = new ObserverStateMachineReference<TSource, TContinuation>(_box, ref _continuation);
                var completion = () => ForwardResult(value, awaiter, ref reference.Target);

                awaiter.OnCompleted(completion);
            }
        }

        public void OnError(Exception error) => _continuation.OnError(error);
        public void OnCompleted() => _continuation.OnCompleted();

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