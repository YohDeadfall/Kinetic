using System;
using System.Reactive.Linq;
using BenchmarkDotNet.Attributes;

namespace Kinetic.Benchmarks;

public partial class LinqWarmCreationBenchmarks
{
    [Benchmark]
    public IDisposable Reactive_ChainOf_1() =>
        _observable
            .Select(_selector)
            .Subscribe(_observer);

    [Benchmark]
    public IDisposable Reactive_ChainOf_2() =>
        _observable
            .Select(_selector)
            .Select(_selector)
            .Subscribe(_observer);

    [Benchmark]
    public IDisposable Reactive_ChainOf_3() =>
        _observable
            .Select(_selector)
            .Select(_selector)
            .Select(_selector)
            .Subscribe(_observer);

    [Benchmark]
    public IDisposable Reactive_ChainOf_4() =>
        _observable
            .Select(_selector)
            .Select(_selector)
            .Select(_selector)
            .Select(_selector)
            .Subscribe(_observer);

    [Benchmark]
    public IDisposable Reactive_ChainOf_5() =>
        _observable
            .Select(_selector)
            .Select(_selector)
            .Select(_selector)
            .Select(_selector)
            .Select(_selector)
            .Subscribe(_observer);

    [Benchmark]
    public IDisposable Reactive_ChainOf_10() =>
        _observable
            .Select(_selector)
            .Select(_selector)
            .Select(_selector)
            .Select(_selector)
            .Select(_selector)
            .Select(_selector)
            .Select(_selector)
            .Select(_selector)
            .Select(_selector)
            .Select(_selector)
            .Subscribe(_observer);
}