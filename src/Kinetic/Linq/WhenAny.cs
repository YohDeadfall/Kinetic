using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Kinetic.Runtime;
using static Kinetic.Linq.WhenAny;

namespace Kinetic.Linq;

internal static class WhenAny
{
    internal readonly struct TaggedValue<T, TTag>(T value)
    {
        public T Value { get; } = value;
    }

    internal readonly struct Source1 { };
    internal readonly struct Source2 { };
    internal readonly struct Source3 { };
    internal readonly struct Source4 { };

    internal static IDisposable Subscribe<T, TTag, TStateMachine>(object source, ref TStateMachine stateMachine)
        where TStateMachine : struct, IStateMachine<ValueTuple>, IObserver<TaggedValue<T, TTag>>
    {
        var tag = new StateMachine<TStateMachine, T, TTag>(ref stateMachine);
        var sub = new SubscribeStateMachine<StateMachine<TStateMachine, T, TTag>, T>(
            tag, (IObservable<T>) source);

        return new ObserverFactory<ValueTuple>()
            .Create<T, SubscribeStateMachine<StateMachine<TStateMachine, T, TTag>, T>>(sub);
    }

    internal static void Dispose(object source)
    {
        if (source is IDisposable disposable)
            disposable.Dispose();
    }

    [StructLayout(LayoutKind.Auto)]
    private readonly struct StateMachine<TContinuation, T, TTag> : IStateMachine<T>
        where TContinuation : struct, IStateMachine<ValueTuple>, IObserver<TaggedValue<T, TTag>>
    {
        private readonly StateMachineValueReference<ValueTuple, TContinuation> _continuation;

        public StateMachine(ref TContinuation continuation) =>
            _continuation = StateMachineValueReference<ValueTuple>.Create(ref continuation);

        public StateMachineBox Box =>
            throw new NotSupportedException();

        public StateMachineReference<T> Reference =>
            throw new NotSupportedException();

        public StateMachineReference? Continuation =>
            throw new NotSupportedException();

        public void Dispose() { }

        public void Initialize(StateMachineBox box) { }

        public void OnCompleted() =>
            CallOnCompleted<TContinuation, T, TTag>(ref _continuation.Target);

        public void OnError(Exception error) =>
            CallOnError<TContinuation, T, TTag>(ref _continuation.Target, error);

        public void OnNext(T value) =>
            _continuation.Target.OnNext(new TaggedValue<T, TTag>(value));
    }

    private static void CallOnCompleted<TThat, T, TTag>(ref TThat that)
        where TThat : struct, IObserver<TaggedValue<T, TTag>>
    {
        that.OnCompleted();
    }

    private static void CallOnError<TThat, T, TTag>(ref TThat that, Exception error)
        where TThat : struct, IObserver<TaggedValue<T, TTag>>
    {
        that.OnError(error);
    }
}

[StructLayout(LayoutKind.Auto)]
public readonly struct WhenAny<T1, T2> : IOperator<(T1, T2)>
{
    private readonly ReadOnlyProperty<T1> _source1;
    private readonly ReadOnlyProperty<T2> _source2;

    public WhenAny(ReadOnlyProperty<T1> source1, ReadOnlyProperty<T2> source2)
    {
        _source1 = source1;
        _source2 = source2;
    }

    public TBox Build<TBox, TBoxFactory, TContinuation>(in TBoxFactory boxFactory, TContinuation continuation)
        where TBoxFactory : struct, IStateMachineBoxFactory<TBox>
        where TContinuation : struct, IStateMachine<(T1, T2)>
    {
        return boxFactory.Create<ValueTuple, StateMachine<TContinuation>>(
            new(continuation, _source1, _source2));
    }

    [StructLayout(LayoutKind.Auto)]
    private struct StateMachine<TContinuation> :
        IEntryStateMachine<ValueTuple>,
        IObserver<TaggedValue<T1, Source1>>,
        IObserver<TaggedValue<T2, Source2>>
        where TContinuation : struct, IStateMachine<(T1, T2)>
    {
        private TContinuation _continuation;
        private object _source1;
        private object _source2;
        private bool _ready;
        [AllowNull]
        private T1 _value1;
        [AllowNull]
        private T2 _value2;

        public StateMachine(TContinuation continuation, ReadOnlyProperty<T1> source1, ReadOnlyProperty<T2> source2)
        {
            _continuation = continuation;
            _source1 = source1.Changed;
            _source2 = source2.Changed;
        }

        public StateMachineBox Box =>
            _continuation.Box;

        public StateMachineReference<ValueTuple> Reference =>
            StateMachineReference<ValueTuple>.Create(ref this);

        public StateMachineReference? Continuation =>
            _continuation.Reference;

        public void Dispose()
        {
            WhenAny.Dispose(_source1);
            WhenAny.Dispose(_source2);
            _continuation.Dispose();
        }

        public void Initialize(StateMachineBox box) =>
            _continuation.Initialize(box);

        public void Start()
        {
            _source1 = Subscribe<T1, Source1, StateMachine<TContinuation>>(_source1, ref this);
            _source2 = Subscribe<T2, Source2, StateMachine<TContinuation>>(_source2, ref this);
            _ready = true;
        }

        public void OnCompleted() =>
            _continuation.OnCompleted();

        public void OnError(Exception error) =>
            _continuation.OnError(error);

        public void OnNext(TaggedValue<T1, Source1> value)
        {
            _value1 = value.Value;

            if (_ready)
                _continuation.OnNext((value.Value, _value2));
        }

        public void OnNext(TaggedValue<T2, Source2> value)
        {
            _value2 = value.Value;
            _continuation.OnNext((_value1, value.Value));
        }

        public void OnNext(ValueTuple value)
        {
        }
    }
}

[StructLayout(LayoutKind.Auto)]
public readonly struct WhenAny<T1, T2, T3> : IOperator<(T1, T2, T3)>
{
    private readonly ReadOnlyProperty<T1> _source1;
    private readonly ReadOnlyProperty<T2> _source2;
    private readonly ReadOnlyProperty<T3> _source3;

    public WhenAny(ReadOnlyProperty<T1> source1, ReadOnlyProperty<T2> source2, ReadOnlyProperty<T3> source3)
    {
        _source1 = source1;
        _source2 = source2;
        _source3 = source3;
    }

    public TBox Build<TBox, TBoxFactory, TContinuation>(in TBoxFactory boxFactory, TContinuation continuation)
        where TBoxFactory : struct, IStateMachineBoxFactory<TBox>
        where TContinuation : struct, IStateMachine<(T1, T2, T3)>
    {
        return boxFactory.Create<ValueTuple, StateMachine<TContinuation>>(
            new(continuation, _source1, _source2, _source3));
    }

    [StructLayout(LayoutKind.Auto)]
    private struct StateMachine<TContinuation> :
        IEntryStateMachine<ValueTuple>,
        IObserver<TaggedValue<T1, Source1>>,
        IObserver<TaggedValue<T2, Source2>>,
        IObserver<TaggedValue<T3, Source3>>
        where TContinuation : struct, IStateMachine<(T1, T2, T3)>
    {
        private TContinuation _continuation;
        private object _source1;
        private object _source2;
        private object _source3;
        private bool _ready;
        [AllowNull]
        private T1 _value1;
        [AllowNull]
        private T2 _value2;
        [AllowNull]
        private T3 _value3;

        public StateMachine(
            TContinuation continuation,
            ReadOnlyProperty<T1> source1,
            ReadOnlyProperty<T2> source2,
            ReadOnlyProperty<T3> source3)
        {
            _continuation = continuation;
            _source1 = source1.Changed;
            _source2 = source2.Changed;
            _source3 = source3.Changed;
        }

        public StateMachineBox Box =>
            _continuation.Box;

        public StateMachineReference<ValueTuple> Reference =>
            StateMachineReference<ValueTuple>.Create(ref this);

        public StateMachineReference? Continuation =>
            _continuation.Reference;

        public void Dispose()
        {
            WhenAny.Dispose(_source1);
            WhenAny.Dispose(_source2);
            WhenAny.Dispose(_source3);
            _continuation.Dispose();
        }

        public void Initialize(StateMachineBox box) =>
            _continuation.Initialize(box);

        public void Start()
        {
            _source1 = Subscribe<T1, Source1, StateMachine<TContinuation>>(_source1, ref this);
            _source2 = Subscribe<T2, Source2, StateMachine<TContinuation>>(_source2, ref this);
            _source3 = Subscribe<T3, Source3, StateMachine<TContinuation>>(_source3, ref this);
            _ready = true;
        }

        public void OnCompleted() =>
            _continuation.OnCompleted();

        public void OnError(Exception error) =>
            _continuation.OnError(error);

        public void OnNext(TaggedValue<T1, Source1> value)
        {
            _value1 = value.Value;

            if (_ready)
                _continuation.OnNext((value.Value, _value2, _value3));
        }

        public void OnNext(TaggedValue<T2, Source2> value)
        {
            _value2 = value.Value;

            if (_ready)
                _continuation.OnNext((_value1, value.Value, _value3));
        }

        public void OnNext(TaggedValue<T3, Source3> value)
        {
            _value3 = value.Value;
            _continuation.OnNext((_value1, _value2, value.Value));
        }

        public void OnNext(ValueTuple value)
        {
        }
    }
}

[StructLayout(LayoutKind.Auto)]
public readonly struct WhenAny<T1, T2, T3, T4> : IOperator<(T1, T2, T3, T4)>
{
    private readonly ReadOnlyProperty<T1> _source1;
    private readonly ReadOnlyProperty<T2> _source2;
    private readonly ReadOnlyProperty<T3> _source3;
    private readonly ReadOnlyProperty<T4> _source4;

    public WhenAny(ReadOnlyProperty<T1> source1, ReadOnlyProperty<T2> source2, ReadOnlyProperty<T3> source3, ReadOnlyProperty<T4> source4)
    {
        _source1 = source1;
        _source2 = source2;
        _source3 = source3;
        _source4 = source4;
    }

    public TBox Build<TBox, TBoxFactory, TContinuation>(in TBoxFactory boxFactory, TContinuation continuation)
        where TBoxFactory : struct, IStateMachineBoxFactory<TBox>
        where TContinuation : struct, IStateMachine<(T1, T2, T3, T4)>
    {
        return boxFactory.Create<ValueTuple, StateMachine<TContinuation>>(
            new(continuation, _source1, _source2, _source3, _source4));
    }

    [StructLayout(LayoutKind.Auto)]
    private struct StateMachine<TContinuation> :
        IEntryStateMachine<ValueTuple>,
        IObserver<TaggedValue<T1, Source1>>,
        IObserver<TaggedValue<T2, Source2>>,
        IObserver<TaggedValue<T4, Source4>>,
        IObserver<TaggedValue<T3, Source3>>
        where TContinuation : struct, IStateMachine<(T1, T2, T3, T4)>
    {
        private TContinuation _continuation;
        private object _source1;
        private object _source2;
        private object _source3;
        private object _source4;
        private bool _ready;
        [AllowNull]
        private T1 _value1;
        [AllowNull]
        private T2 _value2;
        [AllowNull]
        private T3 _value3;
        [AllowNull]
        private T4 _value4;

        public StateMachine(
            TContinuation continuation,
            ReadOnlyProperty<T1> source1,
            ReadOnlyProperty<T2> source2,
            ReadOnlyProperty<T3> source3,
            ReadOnlyProperty<T4> source4)
        {
            _continuation = continuation;
            _source1 = source1.Changed;
            _source2 = source2.Changed;
            _source3 = source3.Changed;
            _source4 = source4.Changed;
        }

        public StateMachineBox Box =>
            _continuation.Box;

        public StateMachineReference<ValueTuple> Reference =>
            StateMachineReference<ValueTuple>.Create(ref this);

        public StateMachineReference? Continuation =>
            _continuation.Reference;

        public void Dispose()
        {
            WhenAny.Dispose(_source1);
            WhenAny.Dispose(_source2);
            WhenAny.Dispose(_source3);
            WhenAny.Dispose(_source4);
            _continuation.Dispose();
        }

        public void Initialize(StateMachineBox box) =>
            _continuation.Initialize(box);

        public void Start()
        {
            _source1 = Subscribe<T1, Source1, StateMachine<TContinuation>>(_source1, ref this);
            _source2 = Subscribe<T2, Source2, StateMachine<TContinuation>>(_source2, ref this);
            _source3 = Subscribe<T3, Source3, StateMachine<TContinuation>>(_source3, ref this);
            _source4 = Subscribe<T4, Source4, StateMachine<TContinuation>>(_source4, ref this);
            _ready = true;
        }

        public void OnCompleted() =>
            _continuation.OnCompleted();

        public void OnError(Exception error) =>
            _continuation.OnError(error);

        public void OnNext(TaggedValue<T1, Source1> value)
        {
            _value1 = value.Value;

            if (_ready)
                _continuation.OnNext((value.Value, _value2, _value3, _value4));
        }

        public void OnNext(TaggedValue<T2, Source2> value)
        {
            _value2 = value.Value;

            if (_ready)
                _continuation.OnNext((_value1, value.Value, _value3, _value4));
        }

        public void OnNext(TaggedValue<T3, Source3> value)
        {
            _value3 = value.Value;

            if (_ready)
                _continuation.OnNext((_value1, _value2, value.Value, _value4));
        }

        public void OnNext(TaggedValue<T4, Source4> value)
        {
            _value4 = value.Value;
            _continuation.OnNext((_value1, _value2, _value3, value.Value));
        }

        public void OnNext(ValueTuple value)
        {
        }
    }
}