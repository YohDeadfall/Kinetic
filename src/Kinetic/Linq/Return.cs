using System;
using System.Runtime.InteropServices;
using Kinetic.Runtime;

namespace Kinetic.Linq;

[StructLayout(LayoutKind.Auto)]
public readonly struct Return<T> : IOperator<T>
{
    public Return(T value) =>
        Value = value;

    public T Value { get; }

    public TBox Build<TBox, TBoxFactory, TContinuation>(in TBoxFactory boxFactory, TContinuation continuation)
        where TBoxFactory : struct, IStateMachineBoxFactory<TBox>
        where TContinuation : struct, IStateMachine<T>
    {
        return boxFactory.Create<T, StateMachine<TContinuation>>(
            new(continuation, Value));
    }

    [StructLayout(LayoutKind.Auto)]
    private struct StateMachine<TContinuation> : IEntryStateMachine<T>
        where TContinuation : struct, IStateMachine<T>
    {
        private TContinuation _continuation;
        private readonly T _value;

        public StateMachineBox Box => throw new NotImplementedException();

        public StateMachineReference<T> Reference => throw new NotImplementedException();

        public StateMachineReference? Continuation => throw new NotImplementedException();

        public StateMachine(TContinuation continuation, T value)
        {
            _continuation = continuation;
            _value = value;
        }

        public void Dispose() =>
            _continuation.Dispose();

        public void Initialize(StateMachineBox box) =>
            _continuation.Initialize(box);

        public void Start()
        {
            _continuation.OnNext(_value);
            _continuation.OnCompleted();
        }

        public void OnCompleted() { }

        public void OnError(Exception error) { }

        public void OnNext(T value) { }
    }
}