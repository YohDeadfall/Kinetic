using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Kinetic.Linq;

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
        public int Number;
        public TestObject Test = new();

        [GlobalSetup]
        public void Setup()
        {
            if (WithSubscribtion)
            {
                Test.Number.Changed.Subscribe(
                    static (value) => { });
            }
        }

        [Benchmark]
        public int Get() => Test.Number;

        [Benchmark]
        public void Set() => Test.Number.Set(Number += 1);

        public sealed class TestObject : Object
        {
            private int _number;

            public Property<int> Number => Property(ref _number);
        }
    }
}