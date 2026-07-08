using Godot;

namespace godottemplate.Content;

/// <summary>
/// Reads the JSON content definition files under <see cref="DefinitionsDir"/>. Each
/// content registry calls <see cref="LoadArray"/> for its file and parses the per-type
/// fields itself (via <see cref="DefReader"/>).
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
    /// Read a definitions file and return its top-level JSON array. Throws a
    /// <see cref="ContentException"/> (and logs it) on a missing file, malformed JSON, or
    /// a non-array root.
    /// </summary>
    public static Godot.Collections.Array LoadArray(string fileName)
    {
        string path = DefinitionsDir + fileName;
        if (!FileAccess.FileExists(path))
            throw ContentException.For(fileName, $"definitions file not found at '{path}'");

        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        if (file == null)
            throw ContentException.For(fileName, $"could not open '{path}': {FileAccess.GetOpenError()}");

        var json = new Json();
        var err = json.Parse(file.GetAsText());
        if (err != Error.Ok)
            throw ContentException.For(fileName, $"malformed JSON at line {json.GetErrorLine()}: {json.GetErrorMessage()}");

        if (json.Data.VariantType != Variant.Type.Array)
            throw ContentException.For(fileName, "expected a top-level JSON array of definitions");

        return json.Data.AsGodotArray();
    }
}
