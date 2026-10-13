using System;
using System.Collections.Generic;

namespace godottemplate.Core.Persistence;

/// <summary>
/// The engine-free logic of a one-file-per-record folder: <c>&lt;dir&gt;&lt;id&gt;.json</c> per
/// record, read and written through an <see cref="IRecordCodec{T}"/> over an
/// <see cref="IRecordFiles"/> backend. Loading is non-fatal: an unreadable, malformed,
/// wrong-version file — or one whose id doesn't match its file name — is skipped with a
/// warning and left where it is. <c>scripts/session/RecordStore.cs</c> is the Godot wrapper
/// that picks the backend and logs the warnings.
/// </summary>
public sealed class RecordFolder<T> where T : class
{
    public const string Extension = ".json";

    private readonly IRecordFiles _files;
    private readonly IRecordCodec<T> _codec;

    /// <summary>The folder, ending in <c>/</c> (e.g. <c>user://saves/</c>).</summary>
    public string Dir { get; }

    public RecordFolder(IRecordFiles files, string dir, IRecordCodec<T> codec)
    {
        _files = files ?? throw new ArgumentNullException(nameof(files));
        _codec = codec ?? throw new ArgumentNullException(nameof(codec));
        if (string.IsNullOrEmpty(dir) || !dir.EndsWith("/", StringComparison.Ordinal))
            throw new ArgumentException($"record folder '{dir}' must end in '/'", nameof(dir));
        Dir = dir;
    }

    /// <summary>Every record that parses, in file-name order, plus a warning for each file that doesn't.</summary>
    public (List<T> Records, List<string> Warnings) Load()
    {
        var records = new List<T>();
        var warnings = new List<string>();
        foreach (var path in _files.List(Dir, Extension))
        {
            var text = _files.Read(path);
            if (text == null)
            {
                warnings.Add($"{path}: unreadable");
                continue;
            }
            var record = _codec.Read(text, path, out var error);
            if (record != null && Dir + _codec.IdOf(record) + Extension != path)
                error = $"{path}: id '{_codec.IdOf(record)}' does not match the file name";
            if (error != null) warnings.Add(error);
            else records.Add(record);
        }
        return (records, warnings);
    }

    /// <summary>Write the record to <c>&lt;id&gt;.json</c>, replacing any file there. Throws on an id that can't be a file name.</summary>
    public void Save(T record)
    {
        var id = _codec.IdOf(record);
        RequireValidId(id);
        _files.Write(PathOf(id), _codec.Write(record));
    }

    /// <summary>Remove the record's file; an absent one is fine.</summary>
    public void Delete(string id)
    {
        RequireValidId(id);
        _files.Delete(PathOf(id));
    }

    public string PathOf(string id) => Dir + id + Extension;

    private static void RequireValidId(string id)
    {
        if (!RecordFolder.IsValidId(id))
            throw new ArgumentException($"record id '{id}' is not a valid file name (ASCII letters, digits, '_', '-'; 1-64 chars)");
    }
}

/// <summary>Non-generic home of the id rule shared by every <see cref="RecordFolder{T}"/>.</summary>
public static class RecordFolder
{
    public const int MaxIdLength = 64;

    /// <summary>
    /// Whether <paramref name="id"/> is safe as a file name on every platform: non-empty,
    /// at most <see cref="MaxIdLength"/> characters of ASCII letters, digits, <c>_</c> and <c>-</c>.
    /// </summary>
    public static bool IsValidId(string id)
    {
        if (string.IsNullOrEmpty(id) || id.Length > MaxIdLength) return false;
        foreach (char c in id)
        {
            bool ok = c is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or (>= '0' and <= '9') or '_' or '-';
            if (!ok) return false;
        }
        return true;
    }
}
