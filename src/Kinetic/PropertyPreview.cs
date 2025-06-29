using Kinetic.Linq;
using Kinetic.Runtime;

namespace Kinetic;

public readonly struct PropertyPreview<T> : IOperator<T>
{
	private readonly ObservableObject.ValueObservable<T> _observable;

	internal PropertyPreview(ObservableObject.ValueObservable<T> observable) =>
		_observable = observable;

    public TBox Build<TBox, TBoxFactory, TContinuation>(in TBoxFactory boxFactory, TContinuation continuation)
        where TBoxFactory : struct, IStateMachineBoxFactory<TBox>
        where TContinuation : struct, IStateMachine<T>
    {
		return boxFactory.Create<T, ObservableObject.ValueObservable<T>.SubscribeStateMachine<TContinuation>>(
			new(continuation, _observable));
    }
}
