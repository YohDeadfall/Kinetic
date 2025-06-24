using System;

namespace Kinetic.Linq;

internal readonly struct FuncTransform<TFrom, TTo> : ITransform<TFrom, TTo>
{
    private readonly Func<TFrom, TTo> _transform;

    public FuncTransform(Func<TFrom, TTo> transfrom) =>
        _transform = transfrom;

    public TTo Transform(TFrom value) =>
        _transform(value);
}