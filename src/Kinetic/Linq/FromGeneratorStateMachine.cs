using System;
using System.Runtime.InteropServices;
using System.Threading;
using Kinetic.Runtime;

namespace Kinetic.Linq;

[StructLayout(LayoutKind.Auto)]
internal struct FromGeneratorStateMachine<T, TContinuation, TGenerator> : IEntryStateMachine<T>
    where TContinuation : struct, IStateMachine<T>
    where TGenerator : struct, ITransform<IObserver<T>, IDisposable>, IDisposable
{
    private TGenerator _generator;
    private TContinuation _continuation;
    private IDisposable? _subscription;

    public FromGeneratorStateMachine(TContinuation continuation, TGenerator generator)
    {
        _generator = generator;
        _continuation = continuation;
        _subscription = Disposable.Empty;
    }

    public StateMachineBox Box =>
        _continuation.Box;

    public StateMachineReference<T> Reference =>
        StateMachineReference<T>.Create(ref this);

    public StateMachineReference? Continuation =>
        _continuation.Reference;

    public void Dispose()
    {
        TryStop();

        _generator.Dispose();
        _continuation.Dispose();
    }

    public void Initialize(StateMachineBox box) =>
        _continuation.Initialize(box);

    public void Start() =>
        _subscription = _generator.Transform(new Observer(ref this));

    private bool TryStop()
    {
        var subscription = Interlocked.Exchange(ref _subscription, null);
        if (subscription is { })
        {
            subscription.Dispose();
            return true;
        }

        return false;
    }

    public void OnCompleted()
    {
        if (TryStop())
            _continuation.OnCompleted();
    }

    public void OnError(Exception error)
    {
        if (TryStop())
            _continuation.OnError(error);
    }

    public void OnNext(T value)
    {
        if (_subscription is { })
            _continuation.OnNext(value);
    }

    private sealed class Observer : IObserver<T>
    {
        private readonly StateMachineValueReference<T, FromGeneratorStateMachine<T, TContinuation, TGenerator>> _stateMachine;

        public Observer(ref FromGeneratorStateMachine<T, TContinuation, TGenerator> stateMachine) =>
            _stateMachine = StateMachineValueReference<T>.Create(ref stateMachine);

        public void OnCompleted() =>
            _stateMachine.Target.OnCompleted();

        public void OnError(Exception error) =>
            _stateMachine.Target.OnError(error);

        public void OnNext(T value) =>
            _stateMachine.Target.OnNext(value);
    }
}