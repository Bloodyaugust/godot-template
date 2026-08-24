using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;

namespace godottemplate.Session;

/// <summary>
/// Disk snapshot of <see cref="PlayerProfile"/> — the DTO that decouples the
/// <c>user://profile.json</c> format from live profile state (CLAUDE.md
/// durable-persistence rules). Missing optional fields deserialize to their
/// defaults, so the schema can grow without a format-version bump.
/// </summary>
public sealed class ProfileSnapshot
{
    [JsonPropertyName("version")] public int Version { get; set; }

    /// <summary>Explicit UI-scale override, or null to follow the monitor-derived auto factor.</summary>
    [JsonPropertyName("scale_override")] public float? ScaleOverride { get; set; }

    /// <summary>Window-mode setting: true = (borderless) fullscreen, false = windowed.</summary>
    [JsonPropertyName("fullscreen")] public bool Fullscreen { get; set; }
}

/// <summary>
/// The profile's companion disk store: one version-stamped
/// <c>user://profile.json</c> record written via <see cref="FileAccess"/> +
/// System.Text.Json. Every failure path is non-fatal — a missing, unreadable,
/// malformed, or version-mismatched file logs a warning and returns null, and
/// the caller falls back to fresh defaults. Purely disk I/O; the memory-only
/// test mode lives on <see cref="PlayerProfile"/>.
/// </summary>
public static class ProfileStore
{
    private const string Path = "user://profile.json";
    private const int FormatVersion = 1;

    /// <summary>The persisted snapshot, or null when there is none usable (fresh defaults apply).</summary>
    public static ProfileSnapshot Load()
    {
        if (!FileAccess.FileExists(Path))
            return null;
        using var file = FileAccess.Open(Path, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            GD.PushWarning($"[Profile] cannot read {Path} ({FileAccess.GetOpenError()}); using defaults");
            return null;
        }
        try
        {
            var snapshot = JsonSerializer.Deserialize<ProfileSnapshot>(file.GetAsText());
            if (snapshot == null || snapshot.Version != FormatVersion)
            {
                GD.PushWarning($"[Profile] {Path} has unsupported format; using defaults");
                return null;
            }
            return snapshot;
        }
        catch (JsonException e)
        {
            GD.PushWarning($"[Profile] {Path} is malformed ({e.Message}); using defaults");
            return null;
        }
    }

    public static void Save(ProfileSnapshot snapshot)
    {
        snapshot.Version = FormatVersion;
        using var file = FileAccess.Open(Path, FileAccess.ModeFlags.Write);
        if (file == null)
        {
            GD.PushWarning($"[Profile] cannot write {Path} ({FileAccess.GetOpenError()})");
            return;
        }
        file.StoreString(JsonSerializer.Serialize(snapshot));
    }
}
