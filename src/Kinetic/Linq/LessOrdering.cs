namespace Kinetic.Linq;

internal readonly struct LessOrdering : IOrdering
{
    public static bool Matches(int ordering) =>
        ordering < 0;
}
