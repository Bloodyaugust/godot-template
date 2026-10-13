namespace godottemplate.Core.Persistence;

/// <summary>
/// The file format of one record type stored one file per record (<c>&lt;id&gt;.json</c>) by
/// <c>scripts/session/RecordStore.cs</c>. Engine-free so the format round-trips under the
/// unit tests. Implement it with <see cref="RecordJson"/>, which stamps and checks the
/// <c>version</c>; <see cref="ExampleRecordCodec"/> is the reference.
/// </summary>
/// <typeparam name="T">The record's DTO snapshot — a plain value, never live state.</typeparam>
public interface IRecordCodec<T> where T : class
{
    /// <summary>The record's id: its file name (without <c>.json</c>). Must satisfy <see cref="RecordFolder.IsValidId"/>.</summary>
    string IdOf(T record);

    /// <summary>The record as version-stamped JSON text.</summary>
    string Write(T record);

    /// <summary>
    /// Parse one file. Non-fatal: a malformed, wrong-version or invalid file returns
    /// <c>null</c> with the reason in <paramref name="error"/> (naming <paramref name="file"/>),
    /// never throws.
    /// </summary>
    T Read(string text, string file, out string error);
}
