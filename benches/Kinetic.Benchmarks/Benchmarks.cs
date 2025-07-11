using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Kinetic.Linq;
using ReactiveUI;

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
    protected NpcTestObject NpcObject = new();
    protected KineticTestObject KineticObject = new();
    protected ReactiveTestObject ReactiveObject = new();

    protected class NpcTestObject : INotifyPropertyChanged
    {
        private int _field;
        public int Property
        {
            get => _field;
            set => Set(ref _field, value);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Set<T>(ref T field, T value, [CallerMemberName] string property = "")
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return;

            field = value;

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
    }

    protected class KineticTestObject : ObservableObject
    {
        private int _field;
        private int _fieldWithHook;

        public Property<int> Property =>
            base.Property(ref _field);

        public Property<int> PropertyWithHook =>
            base.Property(ref _fieldWithHook);

        public KineticTestObject() =>
            Preview<int>(PropertyWithHook, static changing => changing.Select(value => value));
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
    [Benchmark] public int NpcGetter() => NpcObject.Property;
    [Benchmark] public int KineticGetter() => KineticObject.Property;
    [Benchmark] public int KineticGetteriWithHook() => KineticObject.PropertyWithHook;
    [Benchmark] public int ReactiveGetter() => ReactiveObject.Property;
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
            NpcObject.PropertyChanged += (_, _) => { };

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
    [Benchmark] public void NpcSetter() => NpcObject.Property = _value += _change;
    [Benchmark] public void KineticSetter() => KineticObject.Property.Set(_value += _change);
    [Benchmark] public void KineticSetterWithHook() => KineticObject.PropertyWithHook.Set(_value += _change);
    [Benchmark] public void ReactiveSetter() => ReactiveObject.Property = _value += _change;

    private class Observer<T> : IObserver<T>
    {
        public void OnNext(T value) { }
        public void OnError(Exception error) { }
        public void OnCompleted() { }
    }
}