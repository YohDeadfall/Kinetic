namespace Kinetic.Linq;

internal readonly struct GreaterOrdering : IOrdering
{
    public static bool Matches(int ordering) =>
        ordering > 0;
}