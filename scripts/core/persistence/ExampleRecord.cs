namespace godottemplate.Core.Persistence;

/// <summary>
/// Reference record for the one-file-per-record store — a self-documenting stub like
/// <c>scripts/server/IAgentExample.cs</c>, exercised by the unit tests and used by nothing
/// in the game. Copy the pair (record + codec) for a real record type (a saved loadout, a
/// replay index, a player-made design), then delete these.
/// </summary>
public sealed record ExampleRecord(string Id, string Name, int Count);

/// <summary>
/// The reference <see cref="IRecordCodec{T}"/>: <c>{"version": 1, "id": ..., "name": ..., "count": ...}</c>.
/// Usage on the Godot side, one store per folder for the process:
/// <code>
/// static readonly RecordStore&lt;ExampleRecord&gt; Examples = new("user://examples/", new ExampleRecordCodec(), "[Examples]");
/// var all = Examples.LoadAll();            // bad files skipped, each warned once
/// Examples.Save(new ExampleRecord("a", "Alpha", 3)); // save after every mutation
/// </code>
/// </summary>
public sealed class ExampleRecordCodec : IRecordCodec<ExampleRecord>
{
    /// <summary>Bump on an incompatible change; additive optional fields don't need one.</summary>
    public const int FormatVersion = 1;

    public string IdOf(ExampleRecord record) => record.Id;

    public string Write(ExampleRecord record)
        => RecordJson.WriteObject(FormatVersion, w =>
        {
            w.WriteString("id", record.Id);
            w.WriteString("name", record.Name);
            w.WriteNumber("count", record.Count);
        });

    public ExampleRecord Read(string text, string file, out string error)
        => RecordJson.Read(text, file, "example record", FormatVersion, r =>
        {
            r.RejectUnknownKeys("version", "id", "name", "count");
            return new ExampleRecord(r.ReqString("id"), r.ReqString("name"), r.OptInt("count", 0));
        }, out error);
}
