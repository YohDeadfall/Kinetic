namespace Kinetic.Linq;

internal interface IOrdering
{
    static abstract bool Matches(int ordering);
}