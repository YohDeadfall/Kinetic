using System;
using System.Diagnostics;
using Kinetic.Linq.StateMachines;

namespace Kinetic.Linq;

public static partial class ObservableView
{
    public static ObservableView<T> ToView<T>(this ObserverBuilder<ListChange<T>> builder) =>
        ObservableView<T>.Create(builder);
}

public sealed class ObservableView<T> : ReadOnlyObservableList<T>, IDisposable
{
    private IDisposable? _stateMachineBox;

    private ObservableView() { }

    public void Dispose()
    {
        _stateMachineBox?.Dispose();
        _stateMachineBox = null;
    }

    internal static ObservableView<T> Create(ObserverBuilder<ListChange<T>> builder)
    {
        var view = new ObservableView<T>();
        return builder.Build<StateMachine, StateMachineBoxFactory, ObservableView<T>>(
            continuation: new(view), factory: new(view));
    }

    private struct StateMachine : IObserverStateMachine<ListChange<T>>
    {
        private readonly ObservableView<T> _view;

        public StateMachine(ObservableView<T> view) =>
            _view = view;

        public void Dispose() { }
        public void Initialize(ObserverStateMachineBox box) { }

        public void OnCompleted() { }
        public void OnError(Exception error) { }

        public void OnNext(ListChange<T> value)
        {
            switch (value.Action)
            {
                case ListChangeAction.RemoveAll:
                    _view.ClearItems();
                    break;

                case ListChangeAction.Remove:
                    _view.RemoveItemAt(value.OldIndex);
                    break;

                case ListChangeAction.Insert:
                    _view.InsertItem(value.NewIndex, value.NewItem);
                    break;

                case ListChangeAction.Replace:
                    _view.ReplaceItem(value.NewIndex, value.NewItem);
                    break;

                case ListChangeAction.Move:
                    _view.MoveItem(value.OldIndex, value.NewIndex);
                    break;
            }
        }
    }

    private sealed class StateMachineBox<TChange, TStateMachine> : ObserverStateMachineBox<TChange, TStateMachine>, IDisposable
        where TStateMachine : struct, IObserverStateMachine<TChange>
    {
        public StateMachineBox(in TStateMachine stateMachine) :
            base(stateMachine) =>
            StateMachine.Initialize(this);

        public void Dispose() =>
            StateMachine.Dispose();
    }

    private readonly struct StateMachineBoxFactory : IObserverFactory<ObservableView<T>>
    {
        public readonly ObservableView<T> View;

        public StateMachineBoxFactory(ObservableView<T> view) => View = view;

        public ObservableView<T> Create<TSource, TStateMachine>(in TStateMachine stateMachine)
            where TStateMachine : struct, IObserverStateMachine<TSource>
        {
            Debug.Assert(View._stateMachineBox is null);
            View._stateMachineBox = new StateMachineBox<TSource, TStateMachine>(stateMachine);

            return View;
        }
    }
}