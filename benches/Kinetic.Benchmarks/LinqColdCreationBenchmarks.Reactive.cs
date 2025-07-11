using System;
using System.Reactive.Linq;
using BenchmarkDotNet.Attributes;

namespace Kinetic.Benchmarks;

public partial class LinqColdCreationBenchmarks
{
    [Benchmark]
    public IObservable<int> Reactive_ChainOf_1() =>
        _observable
            .Select(_selector);

    [Benchmark]
    public IObservable<int> Reactive_ChainOf_2() =>
        _observable
            .Select(_selector)
            .Select(_selector);

    [Benchmark]
    public IObservable<int> Reactive_ChainOf_3() =>
        _observable
            .Select(_selector)
            .Select(_selector)
            .Select(_selector);

    [Benchmark]
    public IObservable<int> Reactive_ChainOf_4() =>
        _observable
            .Select(_selector)
            .Select(_selector)
            .Select(_selector)
            .Select(_selector);

    [Benchmark]
    public IObservable<int> Reactive_ChainOf_5() =>
        _observable
            .Select(_selector)
            .Select(_selector)
            .Select(_selector)
            .Select(_selector)
            .Select(_selector);

    [Benchmark]
    public IObservable<int> Reactive_ChainOf_10() =>
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
            .Select(_selector);
}