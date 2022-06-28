using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Kinetic.Linq.StateMachines;
using Kinetic.Subjects;
using ReactiveUI;
using KineticLinq = Kinetic.Linq.Observable;
using ReactiveLinq = System.Reactive.Linq.Observable;

namespace Kinetic.Benchmarks;

public class Benchmarks
{
    public static void Main(string[] args) =>
        BenchmarkSwitcher.FromAssembly(typeof(Benchmarks).Assembly).Run(args);
}

[DisassemblyDiagnoser(printSource: true)]
[MemoryDiagnoser]
public abstract class ObjectBenchmarks
{
    protected KineticTestObject KineticObject = new();
    protected ReactiveTestObject ReactiveObject = new();

    protected class KineticTestObject : ObservableObject
    {
        private int _field;
        public Property<int> Property => base.Property(ref _field);
    }

    protected class ReactiveTestObject : ReactiveObject
    {
        private int _field;
        public int Property
        {
            get => _field;
            set => this.RaiseAndSetIfChanged(ref _field, value);
        }
    }
}

public class GetterBenchmarks : ObjectBenchmarks
{
    [Benchmark] public int Kinetic() => KineticObject.Property;
    [Benchmark] public int Reactive() => ReactiveObject.Property;
}

public class SetterBenchmarks : ObjectBenchmarks
{
    private int _value;
    private int _change;

    [GlobalSetup]
    public void Setup()
    {
        if (WithObserver)
        {
            var observer = new Observer<int>();
            KineticObject.Property.Changed
                .Subscribe(observer);
            ReactiveObject
                .WhenAnyValue(self => self.Property)
                .Subscribe(observer);
        }

        _change = WithSameValue ? 0 : 1;
    }

    [Params(false, true)] public bool WithObserver { get; set; }
    [Params(false, true)] public bool WithSameValue { get; set; }
    [Benchmark] public void Kinetic() => KineticObject.Property.Set(_value += _change);
    [Benchmark] public void Reactive() => ReactiveObject.Property = _value += _change;

    private class Observer<T> : IObserver<T>
    {
        public void OnNext(T value) { }
        public void OnError(Exception error) { }
        public void OnCompleted() { }
    }
}

public abstract class LinqBenchmarks
{
    private Subject<int> _kinetic = new();
    private Subject<int> _reactive = new();

    protected abstract ObserverBuilder<int> SetupKinetic(ObserverBuilder<int> source);
    protected abstract IObservable<int> SetupReactive(IObservable<int> source);

    [GlobalSetup]
    public void Setup()
    {
        var kineticBuilder = _kinetic.ToBuilder();
        var reactiveBuilder = _reactive as IObservable<int>;

        for (int index = 0; index < ChainLength; index += 1)
        {
            kineticBuilder = SetupKinetic(kineticBuilder);
            reactiveBuilder = SetupReactive(reactiveBuilder);
        }

        KineticLinq.Subscribe(kineticBuilder, value => { });
        System.ObservableExtensions.Subscribe<int>(reactiveBuilder, value => { });
    }

    [Params(1, 5, 10, 20, 30, 40, 50)] public int ChainLength { get; set; }
    [Benchmark] public void Kinetic() => _kinetic.OnNext(42);
    [Benchmark] public void Reactive() => _reactive.OnNext(42);
}

public class LinqSelectBenchmarks : LinqBenchmarks
{
    protected override ObserverBuilder<int> SetupKinetic(ObserverBuilder<int> source) => KineticLinq.Select(source, static value => value);
    protected override IObservable<int> SetupReactive(IObservable<int> source) => ReactiveLinq.Select(source, static value => value);
}

public class LinqWhereBenchmarks : LinqBenchmarks
{
    protected override ObserverBuilder<int> SetupKinetic(ObserverBuilder<int> source) => KineticLinq.Where(source, static value => value > 0);
    protected override IObservable<int> SetupReactive(IObservable<int> source) => ReactiveLinq.Where(source, static value => value > 0);
}