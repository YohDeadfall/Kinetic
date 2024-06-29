using System;
using Kinetic.Linq.StateMachines;

namespace Kinetic.Linq;

public static partial class Observable
{
    public static ObserverBuilder<TSource[]> ToArray<TSource>(this ObserverBuilder<TSource> source) =>
        source.ContinueWith<ToArrayStateMachineFactory<TSource>, TSource[]>(default);

    public static ObserverBuilder<TSource[]> ToArray<TSource>(this IObservable<TSource> source) =>
        source.ToBuilder().ToArray();

    private readonly struct ToArrayStateMachineFactory<TSource> : IStateMachineFactory<TSource, TSource[]>
    {
        public void Create<TContinuation>(in TContinuation continuation, ObserverStateMachine<TSource> source)
            where TContinuation : struct, IStateMachine<TSource[]>
        {
            source.ContinueWith(new ToArrayStateMachine<TSource, TContinuation>(continuation));
        }
    }

    private struct ToArrayStateMachine<TSource, TContinuation> : IStateMachine<TSource>
        where TContinuation : struct, IStateMachine<TSource[]>
    {
        private TContinuation _continuation;
        private TSource[] _result;
        private int _length;

        public ToArrayStateMachine(in TContinuation continuation)
        {
            _continuation = continuation;
            _result = Array.Empty<TSource>();
        }

        public StateMachineBox Box =>
            _continuation.Box;

        public void Initialize(StateMachineBox box) =>
            _continuation.Initialize(box);

        public void Dispose() =>
            _continuation.Dispose();

        private void IncreareResultLength()
        {
            var length = _length == 0 ? 4 : _length * 2;
            var result = new TSource[length];

            if (_length != 0)
            {
                Array.Copy(_result, result, _length);
            }

            _result = result;
        }

        public void OnNext(TSource value)
        {
            if (_result.Length == _length)
            {
                IncreareResultLength();
            }

            _result[_length] = value;
            _length += 1;
        }

        public void OnError(Exception error) =>
            _continuation.OnError(error);

        public void OnCompleted()
        {
            var result = _result;
            if (result.Length != _length)
            {
                result = new TSource[_length];
                Array.Copy(_result, result, _length);
            }

            _continuation.OnNext(result);
            _continuation.OnCompleted();
        }
    }
}