using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using ReactiveUI;

namespace Kinetic.Benchmarks
{
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
                var observer = new Observer();
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

        private class Observer : IObserver<int>
        {
            public void OnNext(int value) { }
            public void OnError(Exception error) { }
            public void OnCompleted() { }
        }
    }
}