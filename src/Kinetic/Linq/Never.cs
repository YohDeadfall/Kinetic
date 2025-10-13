using System;
using System.Runtime.InteropServices;
using Kinetic.Runtime;

namespace Kinetic.Linq;

[StructLayout(LayoutKind.Auto)]
public readonly struct Never<T> : IOperator<T>
{
    public TBox Build<TBox, TBoxFactory, TContinuation>(in TBoxFactory boxFactory, TContinuation continuation)
        where TBoxFactory : struct, IStateMachineBoxFactory<TBox>
        where TContinuation : struct, IStateMachine<T>
    {
        return boxFactory.Create<T, StateMachine<TContinuation>>(
            new(continuation));
    }

    [StructLayout(LayoutKind.Auto)]
    private struct StateMachine<TContinuation> : IEntryStateMachine<T>
        where TContinuation : struct, IStateMachine<T>
    {
        private TContinuation _continuation;

        public StateMachine(TContinuation continuation) =>
            _continuation = continuation;

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

        public void Start() { }

        public void OnCompleted() { }

        public void OnError(Exception error) { }

        public void OnNext(T value) { }
    }
}