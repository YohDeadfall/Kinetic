using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Kinetic.Runtime;

namespace Kinetic.Linq;

public sealed class ObservableView<T> : ReadOnlyObservableList<T>, IDisposable
{
    [AllowNull]
    private IDisposable _stateMachineBox;

    private ObservableView() { }

    internal static ObservableView<T> Create<TOperator>(Operator<TOperator, ListChange<T>> source)
        where TOperator : IOperator<ListChange<T>>
    {
        var view = new ObservableView<T>();
        view._stateMachineBox = ObserverFactory<ListChange<T>>.Create(new Bind<TOperator>(source, view));
        return view;
    }

    public void Dispose() =>
        _stateMachineBox.Dispose();

    private readonly struct Bind<TOperator> : IOperator<ListChange<T>>
        where TOperator : IOperator<ListChange<T>>
    {
        private readonly TOperator _source;
        private readonly ObservableView<T> _view;

        public Bind(TOperator source, ObservableView<T> view)
        {
            _source = source;
            _view = view;
        }

        public TBox Build<TBox, TBoxFactory, TContinuation>(in TBoxFactory boxFactory, TContinuation continuation)
            where TBoxFactory : struct, IStateMachineBoxFactory<TBox>
            where TContinuation : struct, IStateMachine<ListChange<T>>
        {
            return _source.Build<TBox, TBoxFactory, BindStateMachine<TContinuation>>(
                boxFactory, new(continuation, _view));
        }
    }

    private struct BindStateMachine<TContinuation> : IStateMachine<ListChange<T>>, IReadOnlyList<T>
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

        public StateMachineReference<ListChange<T>> Reference =>
            new ListStateMachineReference<T, BindStateMachine<TContinuation>>(ref this);

        public StateMachineReference? Continuation =>
            _continuation.Reference;

        public int Count =>
            _view.Count;

        public T this[int index] =>
            _view[index];

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

        public IEnumerator<T> GetEnumerator() =>
            _view.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() =>
            _view.GetEnumerator();
    }
}