using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Kinetic.Linq.StateMachines;

namespace Kinetic.Linq;

public static partial class Observable
{
    public static ObserverBuilder<TSource> Throttle<TSource>(this in ObserverBuilder<TSource> source, TimeSpan delay, bool continueOnCapturedContext = true) =>
        source.ContinueWith<ThrottleStateMachineFactory<TSource>, TSource>(new(delay, continueOnCapturedContext));

    public static ObserverBuilder<TSource> Throttle<TSource>(this IObservable<TSource> source, TimeSpan delay, bool continueOnCapturedContext = true) =>
        source.ToBuilder().Throttle(delay, continueOnCapturedContext);

    private readonly struct ThrottleStateMachineFactory<TSource> : IObserverStateMachineFactory<TSource, TSource>
    {
        private readonly TimeSpan _delay;
        private readonly bool _continueOnCapturedContext;

        public ThrottleStateMachineFactory(TimeSpan delay, bool continueOnCapturedContext)
        {
            _delay = delay > TimeSpan.Zero ? delay : throw new ArgumentOutOfRangeException(nameof(delay));
            _continueOnCapturedContext = continueOnCapturedContext;
        }

        public void Create<TContinuation>(in TContinuation continuation, ObserverStateMachine<TSource> source)
            where TContinuation : struct, IObserverStateMachine<TSource>
        {
            source.ContinueWith(new ThrottleStateMachine<TContinuation, TSource>(continuation, _delay, _continueOnCapturedContext));
        }
    }

    private struct ThrottleStateMachine<TContinuation, TSource> : IObserverStateMachine<TSource>
        where TContinuation : struct, IObserverStateMachine<TSource>
    {
        private TContinuation _continuation;
        private ThrottleStateMachinePublisher<TSource> _publisher;

        public ThrottleStateMachine(TContinuation continuation, TimeSpan delay, bool continueOnCapturedContext)
        {
            _continuation = continuation;
            _publisher = new ThrottleStateMachinePublisher<TSource>(delay, continueOnCapturedContext);
        }

        public void Initialize(IObserverStateMachineBox box)
        {
            _continuation.Initialize(box);
        }

        public void Dispose()
        {
            _publisher?.Dispose();
            _continuation.Dispose();
        }

        public void OnNext(TSource value) => _publisher.OnNext(value);

        public void OnError(Exception error) => _continuation.OnError(error);
        public void OnCompleted() => _continuation.OnCompleted();
    }

    private sealed class ThrottleStateMachinePublisher<TSource> : IObservable<TSource>, IDisposable
    {
        private static readonly SendOrPostCallback s_contextCallback = state =>
        {
            var self = Unsafe.As<ThrottleStateMachinePublisher<TSource>>(state);
            try
            {
                self._observer?.OnNext(self._value);
            }
            catch (Exception error)
            {
                self._observer?.OnError(error);
            }
            finally
            {
                Monitor.Exit(self);
            }
        };

        private static readonly TimerCallback s_timerCallback = state =>
        {
            var self = Unsafe.As<ThrottleStateMachinePublisher<TSource>>(state);
            if (self._observer is { } observer)
            {
                var unlock = Monitor.TryEnter(self);
                try
                {
                    if (self._context is { } context)
                    {
                        context.Post(s_contextCallback, self);
                        unlock = false;
                    }
                    else
                    {
                        observer.OnNext(self._value);
                    }
                }
                catch (Exception error)
                {
                    observer.OnError(error);
                }
                finally
                {
                    if (unlock)
                    {
                        Monitor.Exit(self);
                    }
                }
            }
        };

        private readonly Timer _timer;
        private readonly TimeSpan _delay;
        private readonly SynchronizationContext? _context;
        private IObserver<TSource>? _observer;
        private TSource _value;

        public ThrottleStateMachinePublisher(TimeSpan delay, bool continueOnCapturedContext)
        {
            _delay = delay;
            _value = default!;

            _timer = new Timer(s_timerCallback, this, Timeout.Infinite, Timeout.Infinite);
            _context = continueOnCapturedContext ? SynchronizationContext.Current : null;
        }

        public IDisposable Subscribe(IObserver<TSource> observer)
        {
            _observer = _observer is null ? observer : throw new InvalidOperationException();

            return this;
        }

        public void Dispose()
        {
            _observer = null;
            _timer.Dispose();
        }

        public void OnNext(TSource value)
        {
            lock (this)
            {
                _value = value;
                _timer.Change(_delay, Timeout.InfiniteTimeSpan);
            }
        }
    }
}