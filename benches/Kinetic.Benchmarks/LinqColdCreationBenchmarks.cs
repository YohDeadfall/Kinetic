using System;
using BenchmarkDotNet.Attributes;
using Kinetic.Linq;
using Kinetic.Subjects;

namespace Kinetic.Benchmarks;

[MemoryDiagnoser]
public partial class LinqColdCreationBenchmarks
{
    private readonly PublishSubject<int> _observable = new();
    private readonly Func<int, int> _selector = x => x;

    [Benchmark]
    public IObservable<int> Kinetic_ChainOf_1() =>
        _observable
            .Select(_selector);

    [Benchmark]
    public IObservable<int> Kinetic_ChainOf_2() =>
        _observable
            .Select(_selector)
            .Select(_selector);

    [Benchmark]
    public IObservable<int> Kinetic_ChainOf_3() =>
        _observable
            .Select(_selector)
            .Select(_selector)
            .Select(_selector);

    [Benchmark]
    public IObservable<int> Kinetic_ChainOf_4() =>
        _observable
            .Select(_selector)
            .Select(_selector)
            .Select(_selector)
            .Select(_selector);

    [Benchmark]
    public IObservable<int> Kinetic_ChainOf_5() =>
        _observable
            .Select(_selector)
            .Select(_selector)
            .Select(_selector)
            .Select(_selector)
            .Select(_selector);

    [Benchmark]
    public IObservable<int> Kinetic_ChainOf_10() =>
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