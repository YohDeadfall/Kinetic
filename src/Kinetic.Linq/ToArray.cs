using System;
using Kinetic.Linq.StateMachines;

namespace Kinetic.Linq
{
    public static partial class Observable
    {
        public static ObserverBuilder<TResult[]> ToArray<TResult>(this in ObserverBuilder<TResult> source) =>
            source.ContinueWith<ToArrayStateMachineFactory<TResult>, TResult[]>(default);

        public static ObserverBuilder<TResult[]> ToArray<TResult>(this IObservable<TResult> source) =>
            source.ToBuilder().ToArray();

        private readonly struct ToArrayStateMachineFactory<TResult> : IObserverStateMachineFactory<TResult, TResult[]>
        {
            public void Create<TContinuation>(in TContinuation continuation, ObserverStateMachine<TResult> source)
                where TContinuation : struct, IObserverStateMachine<TResult[]>
            {
                source.ContinueWith(new ToArrayStateMachine<TContinuation, TResult>(continuation));
            }
        }

        private struct ToArrayStateMachine<TContinuation, TResult> : IObserverStateMachine<TResult>
            where TContinuation : struct, IObserverStateMachine<TResult[]>
        {
            private TContinuation _continuation;
            private TResult[] _result;
            private int _length;

            public ToArrayStateMachine(in TContinuation continuation)
            {
                _continuation = continuation;
                _result = Array.Empty<TResult>();
                _length = 0;
            }

            public void Initialize(IObserverStateMachineBox box) => _continuation.Initialize(box);
            public void Dispose() => _continuation.Dispose();

            private void IncreareResultLength()
            {
                var length = _length == 0 ? 4 : _length * 2;
                var result = new TResult[length];

                if (_length != 0)
                {
                    Array.Copy(_result, result, _length);
                }

                _result = result;
            }

            public void OnNext(TResult value)
            {
                if (_result.Length == _length)
                {
                    IncreareResultLength();
                }

                _result[_length] = value;
                _length += 1;
            }

            public void OnError(Exception error)
            {
                _continuation.OnError(error);
            }

            public void OnCompleted()
            {
                var result = _result;
                if (result.Length != _length)
                {
                    result = new TResult[_length];
                    Array.Copy(_result, result, _length);
                }

                _continuation.OnNext(result);
                _continuation.OnCompleted();
            }
        }
    }
}