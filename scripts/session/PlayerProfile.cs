using Godot;

namespace godottemplate.Session;

/// <summary>
/// The durable player profile (the CLAUDE.md cross-scene trio's "durable
/// profile" leg): permanent player state that survives the process — today the
/// display settings (UI-scale override + window mode); later currency, unlocks,
/// progression. A static class rather than an autoload: it lazy-loads on first
/// access, so consumers (the <c>DisplayScale</c> autoload at boot) are
/// order-free, and it has no per-frame behavior. Every mutation saves
/// immediately through <see cref="ProfileStore"/>.
///
/// The <c>GODOT_PROFILE_MEMORY</c> env var (any non-empty value) switches the
/// whole profile to memory-only for the process, with fresh defaults. The Hurl
/// runners pin it on every instance they boot, so test runs never touch the
/// developer's real <c>user://profile.json</c>.
/// </summary>
public static class PlayerProfile
{
    private const string EnvMemoryOnly = "GODOT_PROFILE_MEMORY";

    private static bool _loaded;
    private static bool _memoryMode;
    private static float? _scaleOverride;
    private static bool _fullscreen;

    private static void EnsureLoaded()
    {
        if (_loaded) return;
        _loaded = true;

        if (!string.IsNullOrEmpty(OS.GetEnvironment(EnvMemoryOnly)))
        {
            _memoryMode = true;
            return;
        }

        var snapshot = ProfileStore.Load();
        if (snapshot == null) return;
        _scaleOverride = snapshot.ScaleOverride;
        _fullscreen = snapshot.Fullscreen;
    }

    private static void Save()
    {
        if (_memoryMode) return;
        ProfileStore.Save(new ProfileSnapshot
        {
            ScaleOverride = _scaleOverride,
            Fullscreen = _fullscreen,
        });
    }

    /// <summary>Explicit UI-scale override, or null to follow the monitor-derived auto factor.</summary>
    public static float? ScaleOverride
    {
        get { EnsureLoaded(); return _scaleOverride; }
    }

    /// <summary>Window-mode setting: true = (borderless) fullscreen, false = windowed.</summary>
    public static bool Fullscreen
    {
        get { EnsureLoaded(); return _fullscreen; }
    }

    public static void SetScaleOverride(float? factor)
    {
        EnsureLoaded();
        _scaleOverride = factor;
        Save();
    }

    public static void SetFullscreen(bool fullscreen)
    {
        EnsureLoaded();
        _fullscreen = fullscreen;
        Save();
    }
}
