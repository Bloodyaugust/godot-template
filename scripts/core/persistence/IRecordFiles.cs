using System.Collections.Generic;

namespace godottemplate.Core.Persistence;

/// <summary>
/// Where a record folder's files live: <c>user://</c> on disk (the Godot backend in
/// <c>scripts/session/RecordStore.cs</c>) or <see cref="MemoryRecordFiles"/> for a
/// memory-only run. Paths are folder + file name, <c>/</c>-separated, with the folder
/// ending in <c>/</c>.
/// </summary>
public interface IRecordFiles
{
    /// <summary>Every file directly in <paramref name="dir"/> ending in <paramref name="extension"/>, as full paths, ordinal-sorted.</summary>
    IEnumerable<string> List(string dir, string extension);

    /// <summary>The file's text, or null when it can't be read (the backend reports why).</summary>
    string Read(string path);

    void Write(string path, string text);

    /// <summary>Remove the file; a missing file is not an error.</summary>
    void Delete(string path);
}

/// <summary>
/// The in-memory backend: a sorted path → text map. Records still go through their codec's
/// JSON, so a reload in a memory-only run (or a unit test) exercises the real format.
/// </summary>
public sealed class MemoryRecordFiles : IRecordFiles
{
    private readonly SortedDictionary<string, string> _files = new(System.StringComparer.Ordinal);

    public IEnumerable<string> List(string dir, string extension)
    {
        var list = new List<string>();
        foreach (var path in _files.Keys)
        {
            int slash = path.LastIndexOf('/');
            if (path.Substring(0, slash + 1) == dir && path.EndsWith(extension, System.StringComparison.Ordinal))
                list.Add(path);
        }
        return list;
    }

    public string Read(string path) => _files.TryGetValue(path, out var text) ? text : null;
    public void Write(string path, string text) => _files[path] = text;
    public void Delete(string path) => _files.Remove(path);
}
