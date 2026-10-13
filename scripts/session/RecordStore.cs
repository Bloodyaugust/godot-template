using System.Collections.Generic;
using Godot;
using godottemplate.Core.Persistence;

namespace godottemplate.Session;

/// <summary>
/// A durable folder of records, one version-stamped JSON file per record
/// (<c>&lt;dir&gt;&lt;id&gt;.json</c>) — for player-owned collections that outgrow the single
/// <see cref="PlayerProfile"/> record (saved designs, loadouts, replays). The format is an
/// engine-free <see cref="IRecordCodec{T}"/> and the folder logic is
/// <see cref="RecordFolder{T}"/> (<c>scripts/core/persistence/</c>); this class only picks
/// the backend and does Godot file IO. Under <c>GODOT_PROFILE_MEMORY</c>
/// (<see cref="PlayerProfile.MemoryOnly"/>) the files live in memory instead, still through
/// the codec's JSON, so a reload in a test run exercises the real format.
///
/// Reads are non-fatal: a bad file is skipped with a <c>GD.PushWarning</c> and left in
/// place. Save after every durable mutation. Hold one instance per folder for the process
/// (a <c>static readonly</c> field) — in memory mode the instance <em>is</em> the storage.
/// Usage: see <see cref="ExampleRecordCodec"/>.
/// </summary>
public sealed class RecordStore<T> where T : class
{
    private readonly IRecordCodec<T> _codec;
    private readonly string _dir;
    private readonly string _logTag;
    private RecordFolder<T> _folder;

    /// <param name="dir">The folder, ending in <c>/</c> (e.g. <c>user://loadouts/</c>).</param>
    /// <param name="codec">The record's file format.</param>
    /// <param name="logTag">Prefix for warnings, e.g. <c>[Loadouts]</c>.</param>
    public RecordStore(string dir, IRecordCodec<T> codec, string logTag)
    {
        _dir = dir;
        _codec = codec;
        _logTag = logTag;
    }

    // Lazy: PlayerProfile.MemoryOnly reads the env on first use, never in a static initializer.
    private RecordFolder<T> Folder
        => _folder ??= new RecordFolder<T>(
            PlayerProfile.MemoryOnly ? new MemoryRecordFiles() : new DiskFiles(_logTag), _dir, _codec);

    /// <summary>Every record that parses, in file-name order. Each bad file is warned about and skipped.</summary>
    public List<T> LoadAll()
    {
        var (records, warnings) = Folder.Load();
        foreach (var w in warnings) GD.PushWarning($"{_logTag} skipping {w}");
        return records;
    }

    /// <summary>Write one record (replacing its file). Throws on an id that isn't a safe file name — validate ids before they reach here.</summary>
    public void Save(T record) => Folder.Save(record);

    public void Delete(string id) => Folder.Delete(id);

    /// <summary>The <c>user://</c> backend. Every failure is a warning, never an exception.</summary>
    private sealed class DiskFiles : IRecordFiles
    {
        private readonly string _logTag;

        public DiskFiles(string logTag) => _logTag = logTag;

        public IEnumerable<string> List(string dir, string extension)
        {
            var list = new List<string>();
            if (!DirAccess.DirExistsAbsolute(dir)) return list;
            foreach (var name in DirAccess.GetFilesAt(dir))
            {
                if (name.EndsWith(extension)) list.Add(dir + name);
            }
            list.Sort(System.StringComparer.Ordinal);
            return list;
        }

        public string Read(string path)
        {
            using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
            if (file != null) return file.GetAsText();
            GD.PushWarning($"{_logTag} cannot read {path} ({FileAccess.GetOpenError()})");
            return null;
        }

        public void Write(string path, string text)
        {
            var dir = path.GetBaseDir();
            if (!DirAccess.DirExistsAbsolute(dir)) DirAccess.MakeDirRecursiveAbsolute(dir);
            using var file = FileAccess.Open(path, FileAccess.ModeFlags.Write);
            if (file == null)
            {
                GD.PushWarning($"{_logTag} cannot write {path} ({FileAccess.GetOpenError()})");
                return;
            }
            file.StoreString(text);
        }

        public void Delete(string path)
        {
            if (!FileAccess.FileExists(path)) return;
            var err = DirAccess.RemoveAbsolute(path);
            if (err != Error.Ok) GD.PushWarning($"{_logTag} cannot delete {path} ({err})");
        }
    }
}
