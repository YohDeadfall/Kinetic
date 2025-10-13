using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Kinetic.Runtime;

namespace Kinetic.Linq;

[StructLayout(LayoutKind.Auto)]
public readonly struct FromAsync<T> : IOperator<T>
{
    private readonly bool _configureAwait;
    private readonly Func<CancellationToken, ValueTask<T>> _task;

    public FromAsync(Func<CancellationToken, ValueTask<T>> task, bool configureAwait)
    {
        _task = task;
        _configureAwait = configureAwait;
    }

    public TBox Build<TBox, TBoxFactory, TContinuation>(in TBoxFactory boxFactory, TContinuation continuation)
        where TBoxFactory : struct, IStateMachineBoxFactory<TBox>
        where TContinuation : struct, IStateMachine<T>
    {
        return boxFactory.Create<T, StateMachine<TContinuation>>(
            new(continuation, _task, _configureAwait));
    }

    [StructLayout(LayoutKind.Auto)]
    private struct StateMachine<TContinuation> : IEntryStateMachine<T>
        where TContinuation : struct, IStateMachine<T>
    {
        private TContinuation _continuation;
        private readonly bool _configureAwait;
        private readonly CancellationTokenSource _cts;
        private readonly Func<CancellationToken, ValueTask<T>> _task;

        public StateMachine(
            TContinuation continuation,
            Func<CancellationToken, ValueTask<T>> task,
            bool configureAwait)
        {
            _continuation = continuation;
            _configureAwait = configureAwait;
            _cts = new CancellationTokenSource();
            _task = task;
        }

        public StateMachineBox Box =>
            _continuation.Box;

        public StateMachineReference<T> Reference =>
            StateMachineReference<T>.Create(ref this);

        public StateMachineReference? Continuation =>
            _continuation.Reference;

        public void Dispose()
        {
            _cts.Cancel();
            _continuation.Dispose();
        }

        public void Initialize(StateMachineBox box)
        {
            _continuation.Initialize(box);
        }

        public void Start()
        {
            StartCore(StateMachineValueReference<T>.Create(ref this));

            static async void StartCore(StateMachineValueReference<T, StateMachine<TContinuation>> self)
            {
                var that = self.Target;
                try
                {
                    var ct = that._cts.Token;
                    var result = await that._task(ct)
                        .ConfigureAwait(that._configureAwait);

                    that.OnNext(result);
                }
                catch (OperationCanceledException ex)
                when (ex.CancellationToken == that._cts.Token)
                {
                    that.OnCompleted();
                }
                catch (Exception ex)
                {
                    that.OnError(ex);
                }
            }
        }

        public void OnCompleted() =>
            _continuation.OnCompleted();

        public void OnError(Exception error) =>
            _continuation.OnError(error);

        public void OnNext(T value) =>
            _continuation.OnNext(value);
    }
}