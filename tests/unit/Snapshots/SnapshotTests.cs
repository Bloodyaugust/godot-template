using System;
using System.Collections.Generic;
using godottemplate.Core.Snapshots;
using Xunit;

namespace godottemplate.Tests.Snapshots;

public class SnapshotTests
{
    private enum Stage { None, FadePending }

    [Fact]
    public void OfKeepsFieldsInOrder()
    {
        var s = Snapshot.Of(("b", 1), ("a", "x"), ("nested", Snapshot.Of(("ok", true))), ("none", null));
        Assert.Equal(new[] { "b", "a", "nested", "none" }, s.Keys);
        Assert.Equal(1, s["b"]);
        Assert.True((bool)((Dictionary<string, object>)s["nested"])["ok"]);
        Assert.Null(s["none"]);
    }

    [Theory]
    [InlineData("Camel")]
    [InlineData("camelCase")]
    [InlineData("kebab-case")]
    [InlineData("_leading")]
    [InlineData("trailing_")]
    [InlineData("double__underscore")]
    [InlineData("9lives")]
    [InlineData("")]
    public void OfRejectsKeysThatArentSnakeCase(string key)
    {
        Assert.False(Snapshot.IsSnakeCase(key));
        Assert.Throws<ArgumentException>(() => Snapshot.Of((key, 1)));
    }

    [Theory]
    [InlineData("a")]
    [InlineData("min_hold_seconds")]
    [InlineData("v2_layer")]
    public void SnakeCaseKeysPass(string key) => Assert.True(Snapshot.IsSnakeCase(key));

    [Fact]
    public void OfRejectsADuplicateKey() =>
        Assert.Throws<ArgumentException>(() => Snapshot.Of(("a", 1), ("a", 2)));

    [Fact]
    public void SixtyFourBitIntegersMustBeStrings()
    {
        Assert.Throws<ArgumentException>(() => Snapshot.Of(("seed", 1L)));
        Assert.Throws<ArgumentException>(() => Snapshot.Of(("seed", 1UL)));
        Assert.Equal("18446744073709551615", Snapshot.Wide(ulong.MaxValue));
        Assert.Equal("-9223372036854775808", Snapshot.Wide(long.MinValue));
        Assert.Null(Snapshot.Wide((ulong?)null));
    }

    [Fact]
    public void EnumsAreSnakeCaseNames()
    {
        Assert.Throws<ArgumentException>(() => Snapshot.Of(("stage", Stage.None)));
        Assert.Equal("fade_pending", Snapshot.Name(Stage.FadePending));
        Assert.Equal("none", Snapshot.Name(Stage.None));
    }

    [Fact]
    public void OtherTypesAreRefused() =>
        Assert.Throws<ArgumentException>(() => Snapshot.Of(("when", DateTime.UnixEpoch)));

    [Fact]
    public void ListMapsAndChecksEachItem()
    {
        var list = Snapshot.List(new[] { 1, 2 }, i => Snapshot.Of(("i", i)));
        Assert.Equal(2, list.Count);
        Assert.Equal(2, ((Dictionary<string, object>)list[1])["i"]);
        Assert.Empty(Snapshot.List<int>(null, i => i));
        Assert.Throws<ArgumentException>(() => Snapshot.List(new[] { 1L }, l => l));
    }
}
