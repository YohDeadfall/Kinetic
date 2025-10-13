using System;
using System.Runtime.InteropServices;
using System.Threading;
using Kinetic.Runtime;

namespace Kinetic.Linq;

[StructLayout(LayoutKind.Auto)]
public readonly struct FromGeneratorWithCancellation<T> : IOperator<T>
{
    private readonly Func<IObserver<T>, CancellationToken, IDisposable> _subscribe;

    public FromGeneratorWithCancellation(Func<IObserver<T>, CancellationToken, IDisposable> subscribe) =>
        _subscribe = subscribe.ThrowIfArgumentNull();

    public TBox Build<TBox, TBoxFactory, TContinuation>(in TBoxFactory boxFactory, TContinuation continuation)
        where TBoxFactory : struct, IStateMachineBoxFactory<TBox>
        where TContinuation : struct, IStateMachine<T>
    {
        return boxFactory.Create<T, FromGeneratorStateMachine<T, TContinuation, Generator>>(
            new(continuation, new(_subscribe)));
    }

    [StructLayout(LayoutKind.Auto)]
    private readonly struct Generator : ITransform<IObserver<T>, IDisposable>, IDisposable
    {
        private readonly Func<IObserver<T>, CancellationToken, IDisposable> _func;
        private readonly CancellationTokenSource _cts;

        public Generator(Func<IObserver<T>, CancellationToken, IDisposable> func)
        {
            _func = func;
            _cts = new CancellationTokenSource();
        }

        public void Dispose() =>
            _cts.Cancel();

        public IDisposable Transform(IObserver<T> value) =>
            _func(value, _cts.Token);
    }
}