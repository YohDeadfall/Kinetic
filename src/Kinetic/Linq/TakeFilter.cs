namespace Kinetic.Linq;

internal struct TakeFilter<T> : ITransform<T, bool>
{
    private uint _index;

    public TakeFilter(int count) =>
        _index = (uint) count;

    public bool Transform(T value)
    {
        if (_index == 0)
            return false;

        _index = checked(_index - 1);
        return true;
    }
}