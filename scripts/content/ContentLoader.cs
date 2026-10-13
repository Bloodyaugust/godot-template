using System;
using System.Collections.Generic;
using System.Text.Json;
using Godot;
using godottemplate.Core.Content;

namespace godottemplate.Content;

/// <summary>
/// The Godot side of the JSON content load path: reads definition files under
/// <see cref="DefinitionsDir"/> as text and hands that text to the engine-free
/// <see cref="JsonDefReader"/> (<c>scripts/core/content/</c>), which does all the parsing
/// and validation. This class only does what needs Godot — <c>res://</c> file access — and
/// turns an engine-free <see cref="ContentError"/> into a logged <see cref="ContentException"/>.
///
/// All access goes through <see cref="FileAccess"/> on <c>res://</c> paths so it works
/// the same in the editor and in an exported <c>.pck</c> (the JSON files must be marked
/// for export inclusion — see <c>resources/definitions/README.md</c>).
/// </summary>
public static class ContentLoader
{
    /// <summary>Where the authored JSON definition files live.</summary>
    public const string DefinitionsDir = "res://resources/definitions/";

    /// <summary>
    /// The raw text of a definitions file — the reader to inject into an engine-free
    /// <c>Parse(Func&lt;string, string&gt; read)</c> that loads several files at once.
    /// Throws a logged <see cref="ContentException"/> when the file is missing or unreadable.
    /// </summary>
    public static string ReadText(string fileName)
    {
        string path = DefinitionsDir + fileName;
        if (!FileAccess.FileExists(path))
            throw ContentException.For(fileName, $"definitions file not found at '{path}'");

        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        if (file == null)
            throw ContentException.For(fileName, $"could not open '{path}': {FileAccess.GetOpenError()}");
        return file.GetAsText();
    }

    /// <summary>
    /// Read a definitions file and map each element of its top-level JSON array through
    /// <paramref name="each"/> (typically <c>(el, i) =&gt; new ThingDef(new JsonDefReader(el, file, ...))</c>).
    /// Any failure — missing file, malformed JSON, non-array root, or a
    /// <see cref="ContentError"/> thrown by <paramref name="each"/> — surfaces as a logged
    /// <see cref="ContentException"/>.
    /// </summary>
    public static List<T> LoadArray<T>(string fileName, Func<JsonElement, int, T> each)
    {
        string text = ReadText(fileName);
        try
        {
            return JsonDefReader.ParseArray(text, fileName, each);
        }
        catch (ContentError e)
        {
            throw ContentException.From(e);
        }
    }
}
