using System.Collections.Generic;

namespace godottemplate.Core.Content;

// Placeholder definition type: it exists so the content pipeline (examples.json ->
// ExampleContent.Parse -> ContentBootstrap -> /content/state) is exercised at boot, not just
// in unit tests. Replace it with a real definition type — then delete this file,
// resources/definitions/examples.json, its tests and its line in ContentBootstrap — the way
// IAgentExample.cs is a deletable stub.

/// <summary>The closed set of <see cref="ExampleDef.Kind"/> values, written snake_case in JSON.</summary>
public enum ExampleKind { Basic, HeavySupport }

/// <summary>One placeholder definition from <c>examples.json</c>.</summary>
public sealed record ExampleDef(string Id, string Name, int Cost, ExampleKind Kind);

/// <summary>
/// Engine-free parser for <c>examples.json</c>: every entry through a <see cref="JsonDefReader"/>,
/// duplicate ids rejected, file order kept. Any failure throws a <see cref="ContentError"/>.
/// </summary>
public static class ExampleContent
{
    public const string FileName = "examples.json";

    /// <summary>Parse the file's text into its definitions in file order, or throw a <see cref="ContentError"/>.</summary>
    public static IReadOnlyList<ExampleDef> Parse(string json)
    {
        var defs = JsonDefReader.ParseArray(json, FileName, (el, i) =>
        {
            var r = new JsonDefReader(el, FileName, $"example [{i}]");
            r.RejectUnknownKeys("id", "name", "cost", "kind");
            return new ExampleDef(r.ReqString("id"), r.ReqString("name"), r.ReqInt("cost"), r.ReqEnum<ExampleKind>("kind"));
        });
        var seen = new HashSet<string>();
        for (int i = 0; i < defs.Count; i++)
        {
            if (!seen.Add(defs[i].Id))
                throw new ContentError(FileName, $"example [{i}]", $"duplicate example id '{defs[i].Id}'");
        }
        return defs;
    }
}
