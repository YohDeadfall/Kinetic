using System.Runtime.InteropServices;

namespace Kinetic.Linq;

[StructLayout(LayoutKind.Auto)]
internal struct SkipFilter<T> : ITransform<T, bool>
{
    private uint _index;

    public SkipFilter(int count) =>
        _index = (uint) count;

    public bool Transform(T value)
    {
        if (_index == 0)
            return true;

        _index = checked(_index - 1);
        return false;
    }
}