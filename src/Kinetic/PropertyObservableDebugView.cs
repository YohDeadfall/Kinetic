using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Kinetic;

internal sealed class PropertyObservableDebugView<T>
{
    private readonly PropertyObservable<T> _observable;

    public PropertyObservableDebugView(PropertyObservable<T> observable) =>
        _observable = observable;

    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public IReadOnlyList<IObserver<T>> Items =>
        _observable.Subscriptions.GetObserversForDebugger();
}