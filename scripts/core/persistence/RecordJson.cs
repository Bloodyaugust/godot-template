using System;
using System.IO;
using System.Text;
using System.Text.Json;
using godottemplate.Core.Content;

namespace godottemplate.Core.Persistence;

/// <summary>
/// Helpers for an <see cref="IRecordCodec{T}"/>: writes a record as an indented JSON object
/// with a leading <c>version</c> stamp through <see cref="Utf8JsonWriter"/>, and reads one
/// back through the same <see cref="JsonDefReader"/> shipped content uses, turning every
/// failure into an error string instead of an exception.
/// </summary>
public static class RecordJson
{
    /// <summary>Write <c>{"version": <paramref name="version"/>, ...body}</c>.</summary>
    public static string WriteObject(int version, Action<Utf8JsonWriter> body)
    {
        using var stream = new MemoryStream();
        using (var w = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
        {
            w.WriteStartObject();
            w.WriteNumber("version", version);
            body(w);
            w.WriteEndObject();
        }
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    /// <summary>
    /// Parse <paramref name="text"/> as one record object: the <c>version</c> must equal
    /// <paramref name="version"/> (a mismatch is an error, not a migration — add one when it
    /// matters), then <paramref name="parse"/> reads the fields (list <c>"version"</c> among
    /// its known keys when it calls <see cref="JsonDefReader.RejectUnknownKeys"/>). Returns
    /// <c>null</c> and the reason on any failure.
    /// </summary>
    public static T Read<T>(string text, string file, string kind, int version,
        Func<JsonDefReader, T> parse, out string error) where T : class
    {
        error = null;
        try
        {
            using var doc = JsonDocument.Parse(text ?? "");
            var r = new JsonDefReader(doc.RootElement, file, kind);
            int found = r.ReqInt("version");
            if (found != version)
            {
                error = $"{file}: format version {found} is not {version}";
                return null;
            }
            return parse(r);
        }
        catch (JsonException e)
        {
            error = $"{file}: malformed JSON: {e.Message}";
        }
        catch (ContentError e)
        {
            error = e.Message;
        }
        return null;
    }
}
