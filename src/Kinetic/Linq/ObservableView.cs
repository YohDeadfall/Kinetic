using System;
using Kinetic.Linq.StateMachines;

namespace Kinetic.Linq;

public static partial class ObservableView
{
    public static ObservableView<T> ToView<T>(this ObserverBuilder<ListChange<T>> builder) =>
        new ObservableView<T>(builder);
}

public class ObservableView<T> : ReadOnlyObservableList<T>, IDisposable
{
    private readonly IDisposable _stateMachineBox;

    public ObservableView(ObserverBuilder<ListChange<T>> builder) =>
        _stateMachineBox = builder
            .ContinueWith<BindStateMachineFactory, ListChange<T>>(new(this))
            .Subscribe();

    public void Dispose() =>
        _stateMachineBox.Dispose();

    private readonly struct BindStateMachineFactory : IStateMachineFactory<ListChange<T>, ListChange<T>>
    {
        private readonly ObservableView<T> _view;

        public BindStateMachineFactory(ObservableView<T> view) =>
            _view = view;

        public void Create<TContinuation>(in TContinuation continuation, ObserverStateMachine<ListChange<T>> source)
            where TContinuation : struct, IStateMachine<ListChange<T>>
        {
            source.ContinueWith<BindStateMachine<TContinuation>>(new(continuation, _view));
        }
    }

    private struct BindStateMachine<TContinuation> : IStateMachine<ListChange<T>>
        where TContinuation : struct, IStateMachine<ListChange<T>>
    {
        private TContinuation _continuation;
        private readonly ObservableView<T> _view;

        public BindStateMachine(in TContinuation continuation, ObservableView<T> view)
        {
            _continuation = continuation;
            _view = view;
        }

        public StateMachineBox Box =>
            _continuation.Box;

        public void Initialize(StateMachineBox box) =>
            _continuation.Initialize(box);

        public void Dispose() =>
            _continuation.Dispose();

        public void OnCompleted() =>
            _continuation.OnCompleted();

        public void OnError(Exception error) =>
            _continuation.OnError(error);

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
}