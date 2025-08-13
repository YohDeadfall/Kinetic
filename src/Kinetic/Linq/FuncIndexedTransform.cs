using System;

namespace Kinetic.Linq;

internal struct FuncIndexedTransform<TFrom, TTo> : ITransform<TFrom, TTo>
{
    private readonly Func<TFrom, int, TTo> _transform;
    private uint _index;

    public FuncIndexedTransform(Func<TFrom, int, TTo> transform) =>
        _transform = transform;

    public TTo Transform(TFrom value) =>
        _transform(value, checked((int) _index++));
}