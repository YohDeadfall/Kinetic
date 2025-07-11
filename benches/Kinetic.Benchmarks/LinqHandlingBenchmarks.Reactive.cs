using System.Reactive.Linq;
using BenchmarkDotNet.Attributes;
using Kinetic.Subjects;

namespace Kinetic.Benchmarks;

public partial class LinqHandlingBenchmarks
{
    private readonly PublishSubject<int> _reative_1 = CreateSubject(subject => subject
        .Select(Selector)
        .Subscribe(Observer));

    private readonly PublishSubject<int> _reative_2 = CreateSubject(subject => subject
        .Select(Selector)
        .Select(Selector)
        .Subscribe(Observer));

    private readonly PublishSubject<int> _reative_3 = CreateSubject(subject => subject
        .Select(Selector)
        .Select(Selector)
        .Select(Selector)
        .Subscribe(Observer));

    private readonly PublishSubject<int> _reative_4 = CreateSubject(subject => subject
        .Select(Selector)
        .Select(Selector)
        .Select(Selector)
        .Select(Selector)
        .Subscribe(Observer));

    private readonly PublishSubject<int> _reative_5 = CreateSubject(subject => subject
        .Select(Selector)
        .Select(Selector)
        .Select(Selector)
        .Select(Selector)
        .Select(Selector)
        .Subscribe(Observer));

    private readonly PublishSubject<int> _reative_10 = CreateSubject(subject => subject
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
    public void Reactive_ChainOf_1() =>
        _reative_1.OnNext(1);

    [Benchmark]
    public void Reactive_ChainOf_2() =>
        _reative_2.OnNext(2);

    [Benchmark]
    public void Reactive_ChainOf_3() =>
        _reative_3.OnNext(3);

    [Benchmark]
    public void Reactive_ChainOf_4() =>
        _reative_4.OnNext(4);

    [Benchmark]
    public void Reactive_ChainOf_5() =>
        _reative_5.OnNext(5);

    [Benchmark]
    public void Reactive_ChainOf_10() =>
        _reative_10.OnNext(10);

}