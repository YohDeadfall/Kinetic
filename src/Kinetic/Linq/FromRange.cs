using System;
using System.Numerics;
using System.Runtime.InteropServices;
using Kinetic.Runtime;

namespace Kinetic.Linq;

[StructLayout(LayoutKind.Auto)]
public readonly struct FromRange<T>(T start, T end, T step, bool inclusive) : IOperator<T>
    where T : IAdditionOperators<T, T, T>, IComparisonOperators<T, T, bool>
{
    public TBox Build<TBox, TBoxFactory, TContinuation>(in TBoxFactory boxFactory, TContinuation continuation)
        where TBoxFactory : struct, IStateMachineBoxFactory<TBox>
        where TContinuation : struct, IStateMachine<T>
    {
        return boxFactory.Create<T, StateMachine<TContinuation>>(new(continuation, inclusive, start, end, step));
    }

    [StructLayout(LayoutKind.Auto)]
    private struct StateMachine<TContinuation> : IEntryStateMachine<T>
        where TContinuation : struct, IStateMachine<T>
    {
        private TContinuation _continuation;
        private readonly bool _inclusive;
        private readonly T _step;
        private readonly T _start;
        private readonly T _end;

        public StateMachine(TContinuation continuation, bool inclusive, T start, T end, T step)
        {
            _continuation = continuation;
            _inclusive = inclusive;
            _start = start;
            _end = end;
            _step = step;
        }

        public StateMachineBox Box =>
            _continuation.Box;

        public StateMachineReference<T> Reference =>
            StateMachineReference<T>.Create(ref this);

        public StateMachineReference? Continuation =>
            _continuation.Reference;

        public void Dispose() =>
            _continuation.Dispose();

        public void Initialize(StateMachineBox box) =>
            _continuation.Initialize(box);

        public void Start()
        {
            var current = _start;
            while (_inclusive
                ? current <= _end
                : current < _end)
            {
                OnNext(current);

                try
                {
                    checked
                    {
                        current += _step;
                    }
                }
                catch (Exception error)
                {
                    OnError(error);
                }
            }

            OnCompleted();
        }

        public void OnCompleted() =>
            _continuation.OnCompleted();

        public void OnError(Exception error) =>
            _continuation.OnError(error);

        public void OnNext(T value) =>
            _continuation.OnNext(value);
    }
}