using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace Kinetic.Benchmarks
{
    [DisassemblyDiagnoser(printSource: true)]
    [MemoryDiagnoser]
    public class Benchmarks
    {
        public static void Main(string[] args) =>
            BenchmarkRunner.Run<Benchmarks>();

        [Params(false, true)]
        public bool WithSubscribtion;
        public TestObject Test = new();

        [GlobalSetup]
        public void Setup()
        {
            if (WithSubscribtion)
            {
                Test.Number.Changed.Subscribe(
                    new Observer<int>(value => { }));
            }
        }

        [Benchmark]
        public int Get() => Test.Number;

        [Benchmark]
        public void Set() => Test.Number.Set(42);

        public sealed class TestObject : KineticObject
        {
            private int _number;

            public KineticProperty<int> Number => Property(ref _number);
        }

        public sealed class Observer<T> : IObserver<T>
        {
            public Action<T> Handler { get; }
            public Observer(Action<T> handler) => Handler = handler;

            public void OnNext(T value) => Handler(value);
            public void OnError(Exception exception) { }
            public void OnCompleted() { }
        }
    }
}
