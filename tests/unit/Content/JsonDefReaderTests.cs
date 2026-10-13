using System.Collections.Generic;
using godottemplate.Core.Content;
using Xunit;

namespace godottemplate.Tests.Content;

public class JsonDefReaderTests
{
    private enum Category { Collector, ShieldRecharge }

    private sealed record Thing(string Id, int Cost, Category Category, List<string> Tags, (int x, int y) Size);

    private static List<Thing> Parse(string json)
        => JsonDefReader.ParseArray(json, "things.json", (el, i) =>
        {
            var r = new JsonDefReader(el, "things.json", $"thing [{i}]");
            r.RejectUnknownKeys("id", "cost", "category", "tags", "size");
            return new Thing(r.ReqString("id"), r.ReqInt("cost"), r.ReqEnum<Category>("category"),
                r.OptStringList("tags"), r.ReqIntPair("size"));
        });

    [Fact]
    public void ParsesTypedFieldsInFileOrder()
    {
        var things = Parse("""
            [
              {"id": "rig", "cost": 5, "category": "collector", "tags": ["a", "b"], "size": [2, 3]},
              {"id": "cell", "cost": 0, "category": "shield_recharge", "size": [1, 1]}
            ]
            """);
        Assert.Equal(2, things.Count);
        Assert.Equal("rig", things[0].Id);
        Assert.Equal(5, things[0].Cost);
        Assert.Equal(Category.Collector, things[0].Category);
        Assert.Equal(new[] { "a", "b" }, things[0].Tags);
        Assert.Equal((2, 3), things[0].Size);
        Assert.Equal(Category.ShieldRecharge, things[1].Category);
        Assert.Empty(things[1].Tags);
    }

    [Fact]
    public void RejectsUnknownKey()
    {
        var e = Assert.Throws<ContentError>(() =>
            Parse("""[{"id": "rig", "cost": 5, "category": "collector", "size": [1, 1], "cots": 4}]"""));
        Assert.Contains("unknown key 'cots'", e.Message);
        Assert.Equal("things.json", e.File);
        Assert.Equal("thing [0]", e.Context);
    }

    [Theory]
    [InlineData("1.5")]
    [InlineData("\"5\"")]
    [InlineData("1e12")]
    public void RejectsNonInteger(string cost)
    {
        var e = Assert.Throws<ContentError>(() =>
            Parse($$"""[{"id": "rig", "cost": {{cost}}, "category": "collector", "size": [1, 1]}]"""));
        Assert.Contains("'cost' must be an integer", e.Message);
    }

    [Fact]
    public void RejectsUnknownEnumListingTheValidSpellings()
    {
        var e = Assert.Throws<ContentError>(() =>
            Parse("""[{"id": "rig", "cost": 5, "category": "Collector", "size": [1, 1]}]"""));
        Assert.Contains("unknown category 'Collector'", e.Message);
        Assert.Contains("collector, shield_recharge", e.Message);
    }

    [Fact]
    public void RejectsMissingRequiredKey()
    {
        var e = Assert.Throws<ContentError>(() => Parse("""[{"id": "rig", "category": "collector", "size": [1, 1]}]"""));
        Assert.Contains("missing required key 'cost'", e.Message);
    }

    [Theory]
    [InlineData("""[{"id": "rig",}]""")]
    [InlineData("""[/* comment */]""")]
    public void RejectsLaxJson(string json)
    {
        var e = Assert.Throws<ContentError>(() => Parse(json));
        Assert.Contains("malformed JSON", e.Message);
        Assert.Null(e.Context);
    }

    [Fact]
    public void RejectsNonArrayRoot()
    {
        var e = Assert.Throws<ContentError>(() => Parse("""{"id": "rig"}"""));
        Assert.Contains("top-level JSON array", e.Message);
    }

    [Theory]
    [InlineData("ShieldRechargeDelay", "shield_recharge_delay")]
    [InlineData("Add", "add")]
    public void ToSnakeConvertsPascalCase(string pascal, string snake)
        => Assert.Equal(snake, JsonDefReader.ToSnake(pascal));
}
