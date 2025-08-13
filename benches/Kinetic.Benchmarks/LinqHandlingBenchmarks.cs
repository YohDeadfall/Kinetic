using System;
using BenchmarkDotNet.Attributes;
using Kinetic.Linq;
using Kinetic.Subjects;

namespace Kinetic.Benchmarks;

[MemoryDiagnoser]
public partial class LinqHandlingBenchmarks
{
    private static readonly Func<int, int> Selector = x => x;
    private static readonly IObserver<int> Observer = NoOpObserver<int>.Instance;

    private static PublishSubject<int> CreateSubject(Action<PublishSubject<int>> handler)
    {
        var subject = new PublishSubject<int>();
        handler(subject);
        return subject;
    }

    private readonly PublishSubject<int> _kinetic_1 = CreateSubject(subject => subject
        .Select(Selector)
        .Subscribe(Observer));

    private readonly PublishSubject<int> _kinetic_2 = CreateSubject(subject => subject
        .Select(Selector)
        .Select(Selector)
        .Subscribe(Observer));

    private readonly PublishSubject<int> _kinetic_3 = CreateSubject(subject => subject
        .Select(Selector)
        .Select(Selector)
        .Select(Selector)
        .Subscribe(Observer));

    private readonly PublishSubject<int> _kinetic_4 = CreateSubject(subject => subject
        .Select(Selector)
        .Select(Selector)
        .Select(Selector)
        .Select(Selector)
        .Subscribe(Observer));

    private readonly PublishSubject<int> _kinetic_5 = CreateSubject(subject => subject
        .Select(Selector)
        .Select(Selector)
        .Select(Selector)
        .Select(Selector)
        .Select(Selector)
        .Subscribe(Observer));

    private readonly PublishSubject<int> _kinetic_10 = CreateSubject(subject => subject
        .Select(Selector)
        .Select(Selector)
        .Select(Selector)
        .Select(Selector)
        .Select(Selector)
        .Select(Selector)
        .Select(Selector)
        .Select(Selector)
        .Select(Selector)
        .Select(Selector)
        .Subscribe(Observer));

    [Benchmark]
    public void Kinetic_ChainOf_1() =>
        _kinetic_1.OnNext(1);

    [Benchmark]
    public void Kinetic_ChainOf_2() =>
        _kinetic_2.OnNext(2);

    [Benchmark]
    public void Kinetic_ChainOf_3() =>
        _kinetic_3.OnNext(3);

    [Benchmark]
    public void Kinetic_ChainOf_4() =>
        _kinetic_4.OnNext(4);

    [Benchmark]
    public void Kinetic_ChainOf_5() =>
        _kinetic_5.OnNext(5);

    [Benchmark]
    public void Kinetic_ChainOf_10() =>
        _kinetic_10.OnNext(10);

}