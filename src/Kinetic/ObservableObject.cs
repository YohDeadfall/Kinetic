using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Kinetic.Linq;
using Kinetic.Runtime;

namespace Kinetic;

/// <summary>
/// An object with observable properties.
/// </summary>
public abstract class ObservableObject
{
    private PropertyObservable? _observables;
    private uint _suppressions;
    private uint _version;

    protected bool NotificationsEnabled => _suppressions == 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private protected PropertyObservable? GetObservable(IntPtr offset)
    {
        for (var observable = _observables;
            observable is not null;
            observable = observable.Next)
        {
            if (observable.Offset == offset)
            {
                return observable;
            }
        }

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ValueObservable<T>? GetObservableFor<T>(IntPtr offset)
    {
        var observable = GetObservable(offset);

        Debug.Assert(
            observable is null ||
            observable is ValueObservable<T>);

        return Unsafe.As<ValueObservable<T>>(observable);
    }

    private protected PropertyObservable EnsureObservable(IntPtr offset, Func<ObservableObject, IntPtr, PropertyObservable?, PropertyObservable> factory) =>
        GetObservable(offset) ?? CreateObservable(offset, factory);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ValueObservable<T> EnsureObservableFor<T>(IntPtr offset) =>
        GetObservableFor<T>(offset) ?? CreateObservableFor<T>(offset);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private PropertyObservable CreateObservable(IntPtr offset, Func<ObservableObject, IntPtr, PropertyObservable?, PropertyObservable> factory) =>
        _observables = factory(this, offset, _observables);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private ValueObservable<T> CreateObservableFor<T>(IntPtr offset)
    {
        var observable = new ValueObservable<T>(offset, this, _observables);
        _observables = observable;
        return observable;
    }

    /// <summary>
    /// Sets a value of the specified property and notifies the observers.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="property">The property which value will be set.</param>
    /// <param name="value">The value to be set.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void Set<T>(ReadOnlyProperty<T> property, T value)
    {
        CheckOwner(property);
        property.Owner.Set(property.Offset, value);
    }

    /// <summary>
    /// Set a preview handler for the specified property.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="property">The property for which a preview handler should be set..</param>
    /// <param name="preview">The value preview handler that is invoked before setting a new value.</param>
    /// <returns>Returns an observable property for the specified field.</returns>
    protected void Preview<T>(ReadOnlyProperty<T> property, Func<Operator<PropertyPreview<T>, T>, IObservable<T>> preview)
    {
        CheckOwner(property);

        var observable = GetObservableFor<T>(property.Offset);
        if (observable is { })
        {
            throw new InvalidOperationException("A preview handler cannot be set for an already initialized property.");
        }

        EnsureObservable(property.Offset, (owner, offset, next) =>
        {
            var observable = new ValueObservable<T>(offset, owner, next);
            preview(new(new(observable)));
            return observable;
        });
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void CheckOwner<T>(ReadOnlyProperty<T> property)
    {
        if (property.Owner != this)
        {
            throw new ArgumentException("The property belongs to a different object.", nameof(property));
        }
    }

    /// <summary>
    /// Creates an observable property for the specified field.
    /// </summary>
    /// <typeparam name="T">The type of the field.</typeparam>
    /// <param name="field">The field for which an observable propery will be created.</param>
    /// <returns>Returns an observable property for the specified field.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected Property<T> Property<T>(ref T field)
    {
        var offset = GetOffsetOf(ref field);
        return new Property<T>(this, offset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private protected IntPtr GetOffsetOf<T>(ref T field)
    {
        return Unsafe.ByteOffset(
            ref GetReference(),
            ref Unsafe.As<T, IntPtr>(ref field));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ref IntPtr GetReference() =>
        ref Unsafe.As<PropertyObservable?, IntPtr>(ref _observables);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ref T GetReference<T>(IntPtr offset)
    {
        ref var baseRef = ref GetReference();
        ref var valueRef = ref Unsafe.AddByteOffset(ref baseRef, offset);
        return ref Unsafe.As<IntPtr, T>(ref valueRef);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal T Get<T>(IntPtr offset) =>
        GetReference<T>(offset);

    internal void Set<T>(IntPtr offset, T value)
    {
        if (EqualityComparer<T>.Default.Equals(value, Get<T>(offset)))
        {
            return;
        }

        var observable = GetObservableFor<T>(offset);
        if (observable?.Preview is { } preview)
        {
            preview.OnNext(value);
            return;
        }

        GetReference<T>(offset) = value;

        observable?.Changed(value);
    }

    /// <summary>
    /// Suppresses notifications for the current object and returns a
    /// <see cref="SuppressNotificationsScope"/> controlling the time
    /// for which notifications are disabled.
    /// </summary>
    /// <returns>An object serving as a scope of the notification suppression.</returns>
    public SuppressNotificationsScope SuppressNotifications() =>
        new SuppressNotificationsScope(this);

    /// <summary>
    /// A scope controlling the time during notifications are disabled for the object
    /// for which the <see cref="SuppressNotifications"/> method was called.
    /// <summary>
    public readonly struct SuppressNotificationsScope : IDisposable
    {
        private readonly ObservableObject? _owner;

        internal SuppressNotificationsScope(ObservableObject owner)
        {
            if (owner._suppressions == 0)
            {
                owner._version += 1;
            }

            _owner = owner;
            _owner._suppressions++;
        }

        /// <summary>
        /// Enables notifications for the object for which they were previously
        /// suppressed by a call to the <see cref="SuppressNotifications"/> method.
        /// </summary>
        public void Dispose()
        {
            if (_owner is not null &&
                _owner._suppressions-- == 1)
            {
                var version = _owner._version;
                for (var observable = _owner._observables;
                    observable is not null;
                    observable = observable.Next)
                {
                    if (observable.Version == version)
                    {
                        observable.Changed();
                    }
                }
            }
        }
    }

    private sealed class ValueObservableDebugView<T>
    {
        private readonly ValueObservable<T> _observable;

        public ValueObservableDebugView(ValueObservable<T> observable) =>
            _observable = observable;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public IReadOnlyList<IObserver<T>> Items =>
            _observable.Subscriptions.GetObserversForDebugger();
    }

    [DebuggerDisplay("Observers = {Subscriptions.GetObserversCountForDebugger()}")]
    [DebuggerTypeProxy(typeof(ValueObservableDebugView<>))]
    internal sealed class ValueObservable<T> : PropertyObservable, IObservableInternal<T>, IObserver<T>
    {
        internal ObservableSubscriptions<T> Subscriptions;
        internal IObserver<T>? Preview;

        public ValueObservable(IntPtr offset, ObservableObject owner, PropertyObservable? next) :
            base(offset, owner, next)
        { }

        internal override void Changed() =>
            Changed(Owner.Get<T>(Offset));

        internal void Changed(T value) =>
            Subscriptions.OnNext(value);

        public IDisposable Subscribe(IObserver<T> observer)
        {
            observer.OnNext(Owner.Get<T>(Offset));
            return Subscriptions.Subscribe(observer, this);
        }

        public void Subscribe(ObservableSubscription<T> subscription) =>
            Subscriptions.Subscribe(subscription, this);

        public void Unsubscribe(ObservableSubscription<T> subscription) =>
            Subscriptions.Unsubscribe(subscription);

        public void OnCompleted() { }

        public void OnError(Exception error) { }

        public void OnNext(T value) =>
            Owner.Set(Offset, value);

        private void OnPreviewCompleted() { }

        private void OnPreviewError(Exception error) { }

        private void OnPreviewNext(T value)
        {
            if (EqualityComparer<T>.Default.Equals(value, Owner.Get<T>(Offset)))
            {
                return;
            }

            if (Owner.NotificationsEnabled)
            {
                Version = Owner._version++;

                Owner.GetReference<T>(Offset) = value;
                Changed(value);
            }
            else
            {
                Version = Owner._version;
            }
        }

        internal struct SetupStateMachine<TPreview> : IStateMachine<T>
            where TPreview : struct, IStateMachine<T>
        {
            private UpdateStateMachine<TPreview> _update;

            public SetupStateMachine(TPreview preview, ValueObservable<T> observable) =>
                _update = new(preview, observable);

            public StateMachineBox Box =>
                _update.Preview.Box;

            public StateMachineReference<T> Reference =>
                _update.Preview.Reference;

            public StateMachineReference? Continuation =>
                _update.Preview.Continuation;

            public void Dispose()
            {
                _update.Observable.Preview = null;
                _update.Preview.Dispose();
            }

            public void Initialize(StateMachineBox box)
            {
                _update.Preview.Initialize(box);
                _update.Observable.Preview ??= box as IObserver<T> ?? StateMachineReference<T>.Create(ref this);

                ((IObservable<T>) box).Subscribe(_update.Reference);
            }

            public void OnCompleted() =>
                _update.Preview.OnCompleted();

            public void OnError(Exception error) =>
                _update.Preview.OnError(error);

            public void OnNext(T value) =>
                _update.Preview.OnNext(value);
        }

        internal struct UpdateStateMachine<TPreview> : IStateMachine<T>
            where TPreview : struct, IStateMachine<T>
        {
            internal TPreview Preview;
            internal readonly ValueObservable<T> Observable;

            public UpdateStateMachine(TPreview preview, ValueObservable<T> observable)
            {
                Preview = preview;
                Observable = observable;
            }

            public StateMachineBox Box =>
                Preview.Box;

            public StateMachineReference<T> Reference =>
                Preview.Reference;

            public StateMachineReference? Continuation =>
                Preview.Continuation;

            public void Dispose() =>
                throw new NotImplementedException();

            public void Initialize(StateMachineBox box) =>
                throw new NotImplementedException();

            public void OnCompleted() =>
                Observable.OnPreviewCompleted();

            public void OnError(Exception error) =>
                Observable.OnPreviewError(error);

            public void OnNext(T value) =>
                Observable.OnPreviewNext(value);
        }
    }
}

public readonly struct PropertyPreview<T> : IOperator<T>
{
    private readonly ObservableObject.ValueObservable<T> _observable;

    internal PropertyPreview(ObservableObject.ValueObservable<T> observable) =>
        _observable = observable;

    public TBox Build<TBox, TBoxFactory, TContinuation>(in TBoxFactory boxFactory, TContinuation continuation)
        where TBoxFactory : struct, IStateMachineBoxFactory<TBox>
        where TContinuation : struct, IStateMachine<T>
    {
        return boxFactory.Create<T, ObservableObject.ValueObservable<T>.SetupStateMachine<TContinuation>>(
            new(continuation, _observable ?? throw new InvalidOperationException()));
    }
}