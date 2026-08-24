using System;
using godottemplate.Core;
using Xunit;

namespace godottemplate.Tests;

public class GameVersionTests
{
    [Fact]
    public void CurrentIsWellFormed()
    {
        Assert.True(GameVersion.IsValid(GameVersion.Current));
    }

    [Theory]
    [InlineData("0.1.0", "0.2.0", -1)]
    [InlineData("0.2.0", "0.1.0", 1)]
    [InlineData("0.2.0", "0.2.0", 0)]
    [InlineData("0.2.0", "0.10.0", -1)] // numeric, not lexicographic
    [InlineData("1.0.0", "0.99.99", 1)]
    [InlineData("1.2", "1.2.0", 0)] // missing components count as zero
    [InlineData("1.2", "1.2.1", -1)]
    public void CompareOrdersNumerically(string a, string b, int expectedSign)
    {
        Assert.Equal(expectedSign, Math.Sign(GameVersion.Compare(a, b)));
    }

    [Theory]
    [InlineData(null, "0.1.0")]
    [InlineData("", "0.1.0")]
    public void EmptySortsBelowEverything(string empty, string version)
    {
        Assert.True(GameVersion.Compare(empty, version) < 0);
        Assert.True(GameVersion.Compare(version, empty) > 0);
        Assert.Equal(0, GameVersion.Compare(empty, empty));
    }

    [Theory]
    [InlineData("0.2.0-dev")]
    [InlineData("abc")]
    [InlineData("1..0")]
    public void CompareThrowsOnMalformed(string bad)
    {
        Assert.Throws<FormatException>(() => GameVersion.Compare(bad, "0.1.0"));
    }

    [Theory]
    [InlineData("0.1.0", true)]
    [InlineData("10.20.30", true)]
    [InlineData("1.2", true)]
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("0.2.0-dev", false)]
    [InlineData("v0.1.0", false)]
    [InlineData("1..0", false)]
    public void IsValidRecognizesDottedNumeric(string version, bool expected)
    {
        Assert.Equal(expected, GameVersion.IsValid(version));
    }
}
