using System;
using Kinetic.Linq;
using Xunit;

namespace Kinetic.Tests;

public class ValueBitmapTests
{
    [Theory]
    [InlineData("", "11100100111111011010111101100100011001001101100101001001010011101101101011000100101110000101001110110101001001110011000110010101", "")]
    [InlineData("", "1110010011111101101011110110010001100100110110010100100101001110", "1101101011000100101110000101001110110101001001110011000110010101")]
    [InlineData("1110010011111101101011110110010001100100110110010100100101001110", "", "1101101011000100101110000101001110110101001001110011000110010101")]
    [InlineData("11100100111111011010111101100100", "01100100110110010100100101001110110110101100010010111000010100111", "0110101001001110011000110010101")]
    [InlineData("1110010011111101", "101011110110010001100100110110010100100101001110110110101100010010111000010100111011010100100111", "0011000110010101")]
    public void Insert(string left, string middle, string right)
    {
        var bitmap = default(ValueBitmap);

        PopulateBitmap(ref bitmap, left, 0);
        AssertBitmap(bitmap, left);

        PopulateBitmap(ref bitmap, right, left.Length);
        AssertBitmap(bitmap, left + right);

        PopulateBitmap(ref bitmap, middle, left.Length);
        AssertBitmap(bitmap, left + middle + right);
    }

    [Theory]
    [InlineData("")]
    [InlineData("1110010011111101")]
    [InlineData("11100100111111011010111101100100")]
    [InlineData("1110010011111101101011110110010001100100110110010100100101001110")]
    [InlineData("11100100111111011010111101100100011001001101100101001001010011101101101011000100101110000101001110110101001001110011000110010101")]
    public void InsertAtWrongIndexThrows(string data)
    {
        var bitmap = default(ValueBitmap);

        PopulateBitmap(ref bitmap, data, 0);
        Assert.Throws<ArgumentOutOfRangeException>(() => bitmap.Insert(-2, default));
        Assert.Throws<ArgumentOutOfRangeException>(() => bitmap.Insert(-1, default));
        Assert.Throws<ArgumentOutOfRangeException>(() => bitmap.Insert(data.Length + 1, default));
        Assert.Throws<ArgumentOutOfRangeException>(() => bitmap.Insert(data.Length + 2, default));
    }

    [Theory]
    [InlineData("")]
    [InlineData("1110010011111101")]
    [InlineData("11100100111111011010111101100100")]
    [InlineData("1110010011111101101011110110010001100100110110010100100101001110")]
    [InlineData("11100100111111011010111101100100011001001101100101001001010011101101101011000100101110000101001110110101001001110011000110010101")]
    public void RemoveAll(string data)
    {
        var bitmap = default(ValueBitmap);

        PopulateBitmap(ref bitmap, data, 0);

        bitmap.RemoveAll();

        AssertBitmap(bitmap, "");
    }

    [Theory]
    [InlineData("1110010011111101")]
    [InlineData("11100100111111011010111101100100")]
    [InlineData("1110010011111101101011110110010001100100110110010100100101001110")]
    [InlineData("11100100111111011010111101100100011001001101100101001001010011101101101011000100101110000101001110110101001001110011000110010101")]
    public void RemoveAt(string data)
    {
        for (var index = 0; index < data.Length; index += 1)
        {
            var expected = data.Remove(index, 1);
            var bitmap = default(ValueBitmap);

            PopulateBitmap(ref bitmap, data, 0);

            bitmap.RemoveAt(index);

            AssertBitmap(bitmap, expected);
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("1110010011111101")]
    [InlineData("11100100111111011010111101100100")]
    [InlineData("1110010011111101101011110110010001100100110110010100100101001110")]
    [InlineData("11100100111111011010111101100100011001001101100101001001010011101101101011000100101110000101001110110101001001110011000110010101")]
    public void RemoveAtWrongIndexThrows(string data)
    {
        var bitmap = default(ValueBitmap);

        PopulateBitmap(ref bitmap, data, 0);
        Assert.Throws<ArgumentOutOfRangeException>(() => bitmap.RemoveAt(-2));
        Assert.Throws<ArgumentOutOfRangeException>(() => bitmap.RemoveAt(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => bitmap.RemoveAt(data.Length));
        Assert.Throws<ArgumentOutOfRangeException>(() => bitmap.RemoveAt(data.Length + 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => bitmap.RemoveAt(data.Length + 2));
    }

    [Theory]
    [InlineData("1110010011111101")]
    [InlineData("11100100111111011010111101100100")]
    [InlineData("1110010011111101101011110110010001100100110110010100100101001110")]
    [InlineData("11100100111111011010111101100100011001001101100101001001010011101101101011000100101110000101001110110101001001110011000110010101")]
    public void Move(string data)
    {
        for (var fromIndex = 0; fromIndex < data.Length; fromIndex += 1)
            for (var toIndex = 0; toIndex < data.Length; toIndex += 1)
            {
                var expected = data.Remove(fromIndex, 1).Insert(toIndex, data.Substring(fromIndex, 1));
                var bitmap = default(ValueBitmap);

                PopulateBitmap(ref bitmap, data, 0);

                bitmap.Move(fromIndex, toIndex);

                AssertBitmap(bitmap, expected);
            }
    }

    [Theory]
    [InlineData("")]
    [InlineData("1110010011111101")]
    [InlineData("11100100111111011010111101100100")]
    [InlineData("1110010011111101101011110110010001100100110110010100100101001110")]
    [InlineData("11100100111111011010111101100100011001001101100101001001010011101101101011000100101110000101001110110101001001110011000110010101")]
    public void MoveAtWrongIndexThrows(string data)
    {
        var bitmap = default(ValueBitmap);

        PopulateBitmap(ref bitmap, data, 0);
        Assert.Throws<ArgumentOutOfRangeException>(() => bitmap.Move(-2, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => bitmap.Move(-1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => bitmap.Move(0, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => bitmap.Move(0, -2));
        Assert.Throws<ArgumentOutOfRangeException>(() => bitmap.Move(0, data.Length + 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => bitmap.Move(0, data.Length + 2));
        Assert.Throws<ArgumentOutOfRangeException>(() => bitmap.Move(data.Length + 1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => bitmap.Move(data.Length + 2, 0));
    }

    private static void PopulateBitmap(ref ValueBitmap bitmap, string data, int offset)
    {
        for (var index = 0; index < data.Length; index += 1)
            bitmap.Insert(index + offset, data[index] != '0');
    }

    private static void AssertBitmap(in ValueBitmap bitmap, string data)
    {
        var actual = string.Create(bitmap.Length, bitmap, static (span, bitmap) =>
        {
            for (var index = 0; index < bitmap.Length; index += 1)
                span[index] = bitmap[index] ? '1' : '0';
        });

        Assert.Equal(data, actual);
    }

    private static string AsString(in ValueBitmap bitmap) =>
        string.Create(bitmap.Length, bitmap, static (span, bitmap) =>
        {
            for (var index = 0; index < bitmap.Length; index += 1)
                span[index] = bitmap[index] ? '1' : '0';
        });
}