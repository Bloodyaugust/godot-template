using System.IO;
using godottemplate.Core.Content;
using Xunit;

namespace godottemplate.Tests.Content;

public class ExampleContentTests
{
    [Fact]
    public void ParsesRecordsInFileOrder()
    {
        var defs = ExampleContent.Parse("""
            [
              {"id": "a", "name": "A", "cost": 3, "kind": "basic"},
              {"id": "b", "name": "B", "cost": 7, "kind": "heavy_support"}
            ]
            """);
        Assert.Equal(2, defs.Count);
        Assert.Equal(new ExampleDef("a", "A", 3, ExampleKind.Basic), defs[0]);
        Assert.Equal(new ExampleDef("b", "B", 7, ExampleKind.HeavySupport), defs[1]);
    }

    [Fact]
    public void RejectsDuplicateId()
    {
        var e = Assert.Throws<ContentError>(() => ExampleContent.Parse("""
            [
              {"id": "a", "name": "A", "cost": 1, "kind": "basic"},
              {"id": "a", "name": "A2", "cost": 2, "kind": "basic"}
            ]
            """));
        Assert.Equal("examples.json", e.File);
        Assert.Equal("example [1]", e.Context);
        Assert.Contains("duplicate example id 'a'", e.Reason);
    }

    [Fact]
    public void RejectsUnknownKind()
    {
        var e = Assert.Throws<ContentError>(() => ExampleContent.Parse("""
            [{"id": "a", "name": "A", "cost": 1, "kind": "huge"}]
            """));
        Assert.Equal("example [0]", e.Context);
        Assert.Contains("unknown kind 'huge'", e.Reason);
        Assert.Contains("basic, heavy_support", e.Reason);
    }

    [Fact]
    public void ShippedFileParses()
    {
        // The same file the ContentBootstrap autoload loads at boot.
        var path = Path.Combine(RepoRoot(), "resources", "definitions", ExampleContent.FileName);
        Assert.NotEmpty(ExampleContent.Parse(File.ReadAllText(path)));
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(System.AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "project.godot"))) dir = dir.Parent;
        Assert.NotNull(dir);
        return dir.FullName;
    }
}
