using System;
using System.Runtime.InteropServices;
using Kinetic.Runtime;

namespace Kinetic.Linq;

[StructLayout(LayoutKind.Auto)]
public readonly struct FromGenerator<T> : IOperator<T>
{
    private readonly Func<IObserver<T>, IDisposable> _subscribe;

    public FromGenerator(Func<IObserver<T>, IDisposable> subscribe) =>
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
        private readonly Func<IObserver<T>, IDisposable> _func;

        public Generator(Func<IObserver<T>, IDisposable> func) =>
            _func = func;

        public void Dispose() { }

        public IDisposable Transform(IObserver<T> value) =>
            _func(value);
    }
}