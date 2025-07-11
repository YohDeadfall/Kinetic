using System;
using BenchmarkDotNet.Attributes;
using Kinetic.Linq;
using Kinetic.Subjects;

namespace Kinetic.Benchmarks;

[MemoryDiagnoser]
public partial class LinqWarmCreationBenchmarks
{
    private readonly PublishSubject<int> _observable = new();
    private readonly Func<int, int> _selector = x => x;
    private readonly IObserver<int> _observer = NoOpObserver<int>.Instance;

    [Benchmark]
    public IDisposable Kinetic_ChainOf_1() =>
        _observable
            .Select(_selector)
            .Subscribe(_observer);

    [Benchmark]
    public IDisposable Kinetic_ChainOf_2() =>
        _observable
            .Select(_selector)
            .Select(_selector)
            .Subscribe(_observer);

    [Benchmark]
    public IDisposable Kinetic_ChainOf_3() =>
        _observable
            .Select(_selector)
            .Select(_selector)
            .Select(_selector)
            .Subscribe(_observer);

    [Benchmark]
    public IDisposable Kinetic_ChainOf_4() =>
        _observable
            .Select(_selector)
            .Select(_selector)
            .Select(_selector)
            .Select(_selector)
            .Subscribe(_observer);

    [Benchmark]
    public IDisposable Kinetic_ChainOf_5() =>
        _observable
            .Select(_selector)
            .Select(_selector)
            .Select(_selector)
            .Select(_selector)
            .Select(_selector)
            .Subscribe(_observer);

    [Benchmark]
    public IDisposable Kinetic_ChainOf_10() =>
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