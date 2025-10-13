using System;
using System.Runtime.InteropServices;
using System.Threading;
using Kinetic.Runtime;

namespace Kinetic.Linq;

[StructLayout(LayoutKind.Auto)]
public readonly struct ReturnAt<T> : IOperator<T>
{
    private readonly T _value;
    private readonly TimeSpan _dueTime;
    private readonly TimeProvider _timeProvider;
    private readonly CancellationToken _cancellationToken;

    public ReturnAt(T value, TimeSpan dueTime, TimeProvider timeProvider, CancellationToken cancellationToken)
    {
        _value = value;
        _dueTime = dueTime.ThrowIfArgumentNegative();
        _timeProvider = timeProvider.ThrowIfArgumentNull();
        _cancellationToken = cancellationToken;
    }

    public TBox Build<TBox, TBoxFactory, TContinuation>(in TBoxFactory boxFactory, TContinuation continuation)
        where TBoxFactory : struct, IStateMachineBoxFactory<TBox>
        where TContinuation : struct, IStateMachine<T>
    {
        return boxFactory.Create<T, StateMachine<TContinuation>>(
            new(continuation, _value, _dueTime, _timeProvider, _cancellationToken));
    }

    [StructLayout(LayoutKind.Auto)]
    private struct StateMachine<TContinuation> : IEntryStateMachine<T>
        where TContinuation : struct, IStateMachine<T>
    {
        private TContinuation _continuation;
        private readonly T _value;
        private readonly TimeSpan _dueTime;
        private readonly TimeProvider _timeProvider;
        private readonly CancellationToken _cancellationToken;
        private CancellationTokenRegistration _cancellationRegistration;
        private ITimer? _timer;
        private int _completed;

        public StateMachine(TContinuation continuation, T value, TimeSpan dueTime, TimeProvider timeProvider, CancellationToken cancellationToken)
        {
            _continuation = continuation;
            _value = value;
            _dueTime = dueTime;
            _timeProvider = timeProvider;
            _cancellationToken = cancellationToken;
        }

        public StateMachineBox Box =>
            _continuation.Box;

        public StateMachineReference<T> Reference =>
            StateMachineReference<T>.Create(ref this);

        public StateMachineReference? Continuation =>
            _continuation.Reference;

        public void Dispose()
        {
            _timer?.Dispose();
            _continuation.Dispose();
        }

        public void Initialize(StateMachineBox box) =>
            _continuation.Initialize(box);

        public void Start()
        {
            var state = Reference;
            if (_cancellationToken.CanBeCanceled)
            {
                _cancellationRegistration = _cancellationToken.Register(
                    static state =>
                    {
                        ref var self = ref ((StateMachineReference<T, StateMachine<TContinuation>>) state!).Target;
                        var completed = Interlocked.Exchange(ref self._completed, 1) == 1;
                        if (completed) return;

                        self._continuation.OnCompleted();
                    },
                    state);
            }

            static void Callback(object? state)
            {
                ref var self = ref ((StateMachineReference<T, StateMachine<TContinuation>>) state!).Target;
                var completed = Interlocked.Exchange(ref self._completed, 1) == 1;
                if (completed) return;

                self._continuation.OnNext(self._value);
                self._continuation.OnCompleted();
            }
            ;

            if (_timeProvider == TimeProvider.System && _dueTime == TimeSpan.Zero)
            {
                ThreadPool.UnsafeQueueUserWorkItem(Callback, state, preferLocal: false);
            }
            else
            {
                _timer = _timeProvider.CreateTimer(Callback, state, _dueTime, period: TimeSpan.Zero);
            }
        }

        public void OnCompleted() { }

        public void OnError(Exception error) { }

        public void OnNext(T value) { }
    }
}