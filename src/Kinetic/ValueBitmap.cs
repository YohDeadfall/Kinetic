using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Kinetic.Linq;

[StructLayout(LayoutKind.Auto)]
internal struct ValueBitmap
{
    private static readonly int BitsPerBucket = nuint.Size * 8;

    private int _length;
    private nuint _inlineBuckets;
    private nuint[]? _heapBuckets;

    private Span<nuint> Buckets => _heapBuckets is null
        ? MemoryMarshal.CreateSpan(ref _inlineBuckets, 1)
        : _heapBuckets.AsSpan();

    public int Length => _length;

    public bool this[int index]
    {
        get
        {
            if ((uint) index >= (uint) _length)
                throw new ArgumentOutOfRangeException(nameof(index));

            var offset = index / BitsPerBucket;
            var buckets = Buckets;

            return Get(ref buckets[offset], index - offset * BitsPerBucket);
        }
        set
        {
            if ((uint) index >= (uint) _length)
                throw new ArgumentOutOfRangeException(nameof(index));

            var offset = index / BitsPerBucket;
            var buckets = Buckets;

            Set(ref buckets[offset], index - offset * BitsPerBucket, value);
        }
    }

    public void RemoveAll()
    {
        _length = 0;
        _heapBuckets = null;
    }

    public void RemoveAt(int index)
    {
        if ((uint) index >= (uint) _length)
            throw new ArgumentOutOfRangeException(nameof(index));

        var fromOffset = index / BitsPerBucket;
        var toOffset = (_length - 1) / BitsPerBucket;
        var buckets = Buckets;

        ShiftRight(
            fromBucket: ref buckets[fromOffset],
            fromIndex: index - fromOffset * BitsPerBucket,
            toBucket: ref buckets[toOffset],
            toIndex: BitsPerBucket - 1);

        _length -= 1;
    }

    public void Move(int fromIndex, int toIndex)
    {
        if ((uint) fromIndex >= (uint) _length)
            throw new ArgumentOutOfRangeException(nameof(fromIndex));

        if ((uint) toIndex >= (uint) _length)
            throw new ArgumentOutOfRangeException(nameof(toIndex));

        if (fromIndex == toIndex)
            return;

        var shiftLeft = fromIndex > toIndex;
        var fromOffset = fromIndex / BitsPerBucket;
        var toOffset = toIndex / BitsPerBucket;
        var buckets = Buckets;

        ref var fromBucket = ref buckets[fromOffset];
        ref var toBucket = ref buckets[toOffset];

        fromIndex -= fromOffset * BitsPerBucket;
        toIndex -= toOffset * BitsPerBucket;

        var value = Get(ref fromBucket, fromIndex);

        if (shiftLeft)
        {
            ShiftLeft(ref toBucket, toIndex, ref fromBucket, fromIndex);
        }
        else
        {
            ShiftRight(ref fromBucket, fromIndex, ref toBucket, toIndex);
        }

        Set(ref toBucket, toIndex, value);
    }

    public void Insert(int index, bool item)
    {
        if ((uint) index > (uint) _length)
            throw new ArgumentOutOfRangeException(nameof(index));

        var offset = index / BitsPerBucket;
        var buckets = Buckets;

        if (buckets.Length * BitsPerBucket <= _length)
        {
            var arraySize = Math.Min(4, buckets.Length * 2);
            var array = new nuint[arraySize];

            buckets.CopyTo(array);
            buckets = array;

            _heapBuckets = array;
        }

        ref var bucket = ref buckets[offset];

        if (index < _length)
        {
            // Not reducing the length since then next bucket
            // might be used to store the uppermost bit.
            var endOffset = _length / BitsPerBucket;

            ShiftLeft(
                fromBucket: ref bucket,
                fromIndex: index - offset * BitsPerBucket,
                toBucket: ref buckets[endOffset],
                toIndex: BitsPerBucket - 1);
        }

        Set(ref bucket, index - offset * BitsPerBucket, item);

        _length += 1;
    }

    public int PopCountBefore(int index)
    {
        if ((uint) index > (uint) _length)
            throw new ArgumentOutOfRangeException(nameof(index));

        if (index == 0)
            return 0;

        var offset = index / BitsPerBucket;
        var buckets = Buckets;

        ref var fromBucket = ref buckets[0];
        ref var toBucket = ref buckets[offset];

        var result = nuint.PopCount(toBucket & (nuint.MaxValue >> (BitsPerBucket + BitsPerBucket * offset - index)));

        while (Unsafe.IsAddressGreaterThan(ref fromBucket, ref toBucket))
        {
            result += nuint.PopCount(fromBucket);
            fromBucket = ref Unsafe.Add(ref fromBucket, 1);
        }

        return (int) result;
    }

    private static bool Get(ref nuint bucket, int index) =>
        (bucket & (((nuint) 1) << index)) != 0;

    private static void Set(ref nuint bucket, int index, bool value)
    {
        nuint bit = ((nuint) 1) << index;

        bucket = value
            ? bucket | bit
            : bucket & ~bit;
    }

    private static void ShiftLeft(ref nuint fromBucket, int fromIndex, ref nuint toBucket, int toIndex)
    {
        Debug.Assert(fromIndex < BitsPerBucket);
        Debug.Assert(toIndex < BitsPerBucket);

        var accumulator = toBucket;

        if (Unsafe.AreSame(ref fromBucket, ref toBucket))
        {
            var stay = UpperBits(toIndex + 1) | LowerBits(fromIndex);
            var move = UpperBits(fromIndex) & LowerBits(toIndex + 1);

            toBucket = (accumulator & stay) | ((accumulator << 1) & move);
        }
        else
        {
            var toStay = UpperBits(toIndex + 1);
            var toMove = LowerBits(toIndex + 1);

            accumulator = (accumulator & toStay) | ((accumulator << 1) & toMove);

            while (true)
            {
                var current = Unsafe.Add(ref toBucket, -1);

                toBucket = accumulator | (current >> (BitsPerBucket - 1));
                toBucket = ref Unsafe.Add(ref toBucket, -1);

                accumulator = current;

                if (Unsafe.AreSame(ref fromBucket, ref toBucket))
                {
                    break;
                }

                accumulator <<= 1;
            }

            var fromStay = LowerBits(fromIndex);
            var fromMove = UpperBits(fromIndex);

            toBucket = (accumulator & fromStay) | ((accumulator << 1) & fromMove);
        }
    }

    private static void ShiftRight(ref nuint fromBucket, int fromIndex, ref nuint toBucket, int toIndex)
    {
        Debug.Assert(fromIndex < BitsPerBucket);
        Debug.Assert(toIndex < BitsPerBucket);

        var accumulator = fromBucket;

        if (Unsafe.AreSame(ref fromBucket, ref toBucket))
        {
            var stay = UpperBits(toIndex + 1) | LowerBits(fromIndex);
            var move = UpperBits(fromIndex) & LowerBits(toIndex + 1);

            toBucket = (accumulator & stay) | ((accumulator >> 1) & move);
        }
        else
        {
            var fromStay = LowerBits(fromIndex);
            var fromMove = UpperBits(fromIndex);

            accumulator = (accumulator & fromStay) | ((accumulator >> 1) & fromMove);

            while (true)
            {
                var current = Unsafe.Add(ref fromBucket, 1);

                fromBucket = accumulator | (current << (BitsPerBucket - 1));
                fromBucket = ref Unsafe.Add(ref fromBucket, 1);

                accumulator = current;

                if (Unsafe.AreSame(ref fromBucket, ref toBucket))
                {
                    break;
                }

                accumulator >>= 1;
            }

            var toStay = UpperBits(toIndex);
            var toMove = LowerBits(toIndex);

            fromBucket = (accumulator & toStay) | ((accumulator >> 1) & toMove);
        }
    }

    private static nuint UpperBits(int position) =>
        position == BitsPerBucket ? 0 : nuint.MaxValue << position;

    private static nuint LowerBits(int position) =>
        position == 0 ? 0 : nuint.MaxValue >> BitsPerBucket - position;
}