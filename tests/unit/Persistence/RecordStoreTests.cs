using System;
using godottemplate.Core.Persistence;
using Xunit;

namespace godottemplate.Tests.Persistence;

public class RecordStoreTests
{
    private const string Dir = "user://examples/";
    private readonly ExampleRecordCodec _codec = new();

    [Fact]
    public void CodecRoundTripsAndStampsTheVersion()
    {
        var record = new ExampleRecord("alpha", "Alpha \"one\"", 3);
        var text = _codec.Write(record);
        Assert.Contains("\"version\": 1", text);
        Assert.Equal(record, _codec.Read(text, "alpha.json", out var error));
        Assert.Null(error);
    }

    [Theory]
    [InlineData("{\"version\": 2, \"id\": \"a\", \"name\": \"A\"}", "format version 2 is not 1")]
    [InlineData("{\"id\": \"a\", \"name\": \"A\"}", "missing required key 'version'")]
    [InlineData("{\"version\": 1, \"id\": \"a\", \"name\": \"A\", \"extra\": 1}", "unknown key 'extra'")]
    [InlineData("{\"version\": 1, \"id\": \"a\"", "malformed JSON")]
    [InlineData("", "malformed JSON")]
    public void BadFileReadsAsNullWithAReason(string text, string reason)
    {
        Assert.Null(_codec.Read(text, "a.json", out var error));
        Assert.Contains(reason, error);
        Assert.Contains("a.json", error);
    }

    [Fact]
    public void FolderSavesLoadsAndDeletesThroughTheCodec()
    {
        var files = new MemoryRecordFiles();
        var folder = new RecordFolder<ExampleRecord>(files, Dir, _codec);
        folder.Save(new ExampleRecord("b", "Bravo", 2));
        folder.Save(new ExampleRecord("a", "Alpha", 1));

        var (records, warnings) = new RecordFolder<ExampleRecord>(files, Dir, _codec).Load();
        Assert.Empty(warnings);
        Assert.Equal(new[] { "a", "b" }, records.ConvertAll(r => r.Id));

        folder.Delete("a");
        folder.Delete("never_saved");
        Assert.Equal(new[] { "b" }, folder.Load().Records.ConvertAll(r => r.Id));
    }

    [Fact]
    public void BadFilesAreSkippedWithAWarningAndLeftInPlace()
    {
        var files = new MemoryRecordFiles();
        var folder = new RecordFolder<ExampleRecord>(files, Dir, _codec);
        folder.Save(new ExampleRecord("good", "Good", 1));
        files.Write(Dir + "broken.json", "{ not json");
        files.Write(Dir + "renamed.json", _codec.Write(new ExampleRecord("original", "Moved", 1)));
        files.Write(Dir + "notes.txt", "ignored: wrong extension");
        files.Write("user://other/elsewhere.json", _codec.Write(new ExampleRecord("elsewhere", "X", 1)));

        var (records, warnings) = folder.Load();
        Assert.Equal(new[] { "good" }, records.ConvertAll(r => r.Id));
        Assert.Equal(2, warnings.Count);
        Assert.Contains("malformed JSON", warnings[0]);
        Assert.Contains("does not match the file name", warnings[1]);
        Assert.NotNull(files.Read(Dir + "broken.json"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("../escape")]
    [InlineData("a/b")]
    [InlineData("has space")]
    public void UnsafeIdsAreRefused(string id)
    {
        var folder = new RecordFolder<ExampleRecord>(new MemoryRecordFiles(), Dir, _codec);
        Assert.False(RecordFolder.IsValidId(id));
        Assert.Throws<ArgumentException>(() => folder.Save(new ExampleRecord(id, "X", 0)));
    }

    [Fact]
    public void FolderMustEndInASlash()
        => Assert.Throws<ArgumentException>(() => new RecordFolder<ExampleRecord>(new MemoryRecordFiles(), "user://examples", _codec));
}
