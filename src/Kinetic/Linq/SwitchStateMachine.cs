using System;
using Kinetic.Runtime;

namespace Kinetic.Linq;

internal struct SwitchStateMachine<TContinuation, TSource> : IStateMachine<IObservable<TSource>?>
    where TContinuation : struct, IStateMachine<TSource>
{
    private TContinuation _continuation;
    private IDisposable? _subscription;
    private StateMachineReference<IObservable<TSource>?, SwitchStateMachine<TContinuation, TSource>>? _self;
    private readonly object _gate = new();

    public SwitchStateMachine(TContinuation continuation) =>
        _continuation = continuation;

    public StateMachineBox Box =>
        _continuation.Box;

    public StateMachineReference<IObservable<TSource>?> Reference =>
        StateMachineReference<IObservable<TSource>?>.Create(ref this);

    public StateMachineReference? Continuation =>
        _continuation.Reference;

    public void Dispose()
    {
        _subscription?.Dispose();
        _continuation.Dispose();
    }

    public void Initialize(StateMachineBox box) =>
        _continuation.Initialize(box);

    public void OnCompleted()
    {
        lock (_gate)
        {
            if (_subscription is null)
            {
                _continuation.OnCompleted();
                _self = null;
            }
        }
    }

    public void OnError(Exception error) =>
        _continuation.OnError(error);

    public void OnNext(IObservable<TSource>? value)
    {
        lock (_gate)
        {
            _subscription?.Dispose();
            _subscription = value?
                .Subscribe()
                .Build<IDisposable, ObserverFactory<TSource>, Inner>(
                    new(), new(_self = StateMachineReference<IObservable<TSource>?>.Create(ref this)));
        }
    }

    private struct Inner : IStateMachine<TSource>
    {
        private readonly StateMachineReference<IObservable<TSource>?, SwitchStateMachine<TContinuation, TSource>> _outer;

        public Inner(StateMachineReference<IObservable<TSource>?, SwitchStateMachine<TContinuation, TSource>> outer)
        {
            _outer = outer;
        }

        public StateMachineBox Box => throw new NotImplementedException();

        public StateMachineReference<TSource> Reference => throw new NotImplementedException();

        public StateMachineReference? Continuation => throw new NotImplementedException();

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void Initialize(StateMachineBox box)
        {
            throw new NotImplementedException();
        }

        public void OnCompleted()
        {
            ref var outer = ref _outer.Target;
            lock (outer._gate)
            {
                if (outer._self == _outer)
                    outer._continuation.OnCompleted();
            }
        }

        public void OnError(Exception error)
        {
            ref var outer = ref _outer.Target;
            lock (outer._gate)
            {
                if (outer._self == _outer)
                    outer._continuation.OnError(error);
            }
        }

        public void OnNext(TSource value)
        {
            ref var outer = ref _outer.Target;
            lock (outer._gate)
            {
                if (outer._self == _outer)
                    outer._continuation.OnNext(value);
            }
        }
    }
}