using System;

namespace Kinetic.Linq
{
    public static partial class Observable
    {
        public static ObserverBuilder<TSource> SkipWhile<TSource>(this in ObserverBuilder<TSource> source, Func<TSource, bool> predicate) =>
            source.ContinueWith<SkipWhileStateMachineFactory<TSource>, TSource>(new(predicate));

        public static ObserverBuilder<TSource> SkipWhile<TSource>(this IObservable<TSource> source, Func<TSource, bool> predicate) =>
            source.ToBuilder().SkipWhile(predicate);
    }

    internal readonly struct SkipWhileStateMachineFactory<TSource> : IObserverStateMachineFactory<TSource, TSource>
    {
        private readonly Func<TSource, bool> _predicate;

        public SkipWhileStateMachineFactory(Func<TSource, bool> predicate)
        {
            _predicate = predicate;
        }

        public void Create<TContinuation>(in TContinuation continuation, ObserverStateMachine<TSource> source)
            where TContinuation : struct, IObserverStateMachine<TSource>
        {
            source.ContinueWith(new SkipWhileStateMachine<TContinuation, TSource>(continuation, _predicate));
        }
    }

    internal struct SkipWhileStateMachine<TContinuation, TSource> : IObserverStateMachine<TSource>
        where TContinuation : IObserverStateMachine<TSource>
    {
        private TContinuation _continuation;
        private Func<TSource, bool>? _predicate;

        public SkipWhileStateMachine(TContinuation continuation, Func<TSource, bool> predicate)
        {
            _continuation = continuation;
            _predicate = predicate;
        }

        public void Initialize(IObserverStateMachineBox box) => _continuation.Initialize(box);
        public void Dispose() => _continuation.Dispose();

        public void OnNext(TSource value)
        {
            try
            {
                if (_predicate?.Invoke(value) != true)
                {
                    _predicate = null;
                    _continuation.OnNext(value);
                }
            }
            catch (Exception error)
            {
                _continuation.OnError(error);
            }
        }

        public void OnError(Exception error) => _continuation.OnError(error);
        public void OnCompleted() => _continuation.OnCompleted();
    }
}