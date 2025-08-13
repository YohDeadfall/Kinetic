using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Kinetic.Linq;

internal sealed class ThrottleStateMachinePublisher<TSource> : IObservable<TSource>, IDisposable
{
    private static readonly SendOrPostCallback s_contextCallback = state =>
    {
        var self = Unsafe.As<ThrottleStateMachinePublisher<TSource>>(state!);
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
        var self = Unsafe.As<ThrottleStateMachinePublisher<TSource>>(state!);
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

    [AllowNull]
    private TSource _value;

    public ThrottleStateMachinePublisher(TimeSpan delay, bool continueOnCapturedContext)
    {
        _delay = delay;
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