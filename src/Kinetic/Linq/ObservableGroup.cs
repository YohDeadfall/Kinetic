namespace Kinetic.Linq;

public class ObservableGroup<TKey, TElement> : ObservableView<TElement>
{
    public TKey Key { get; }

    protected internal ObservableGroup(TKey key) =>
        Key = key;
}