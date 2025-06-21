namespace Kinetic.Linq;

internal interface ITransform<TFrom, TTo>
{
    TTo Transform(TFrom value);
}