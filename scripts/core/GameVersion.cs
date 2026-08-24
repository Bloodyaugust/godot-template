using System;

namespace godottemplate.Core;

/// <summary>
/// The single authoritative game version. Everything else derives from
/// <see cref="Current"/>: the release pipeline verifies the pushed tag against
/// it and stamps <c>project.godot</c> <c>config/version</c> from it (the
/// committed config value is metadata, not truth), UI display and persistence
/// stamps read it, and anything ordering versions (a changelog seen-marker, a
/// net handshake) goes through <see cref="Compare"/>. This class stays
/// engine-free, so surfaces that show or exchange the version append
/// <c>-dev</c> themselves in debug builds (<c>OS.IsDebugBuild()</c> — see the
/// <c>/status</c> handler in <c>AgentRestServer</c>) so a mid-development
/// build never matches a released build of the same number.
/// Bump it as the first step of cutting a release.
/// </summary>
public static class GameVersion
{
    public const string Current = "0.1.0";

    /// <summary>
    /// Orders two dotted-numeric version strings (semver-shaped, no
    /// prerelease/build suffixes — version comparisons only ever handle bare
    /// numbers). Missing components count as zero ("1.2" == "1.2.0");
    /// null/empty sorts below everything (the never-seen sentinel). Returns
    /// &lt;0, 0, or &gt;0 like <see cref="string.CompareTo(string)"/>.
    /// </summary>
    /// <exception cref="FormatException">A non-empty component is not a plain integer.</exception>
    public static int Compare(string a, string b)
    {
        bool aEmpty = string.IsNullOrEmpty(a);
        bool bEmpty = string.IsNullOrEmpty(b);
        if (aEmpty || bEmpty)
            return aEmpty == bEmpty ? 0 : (aEmpty ? -1 : 1);

        int[] aParts = ParseAll(a);
        int[] bParts = ParseAll(b);
        int length = Math.Max(aParts.Length, bParts.Length);
        for (int i = 0; i < length; i++)
        {
            int aNum = i < aParts.Length ? aParts[i] : 0;
            int bNum = i < bParts.Length ? bParts[i] : 0;
            if (aNum != bNum)
                return aNum < bNum ? -1 : 1;
        }
        return 0;
    }

    /// <summary>True when <paramref name="version"/> is a well-formed dotted-numeric version.</summary>
    public static bool IsValid(string version)
    {
        if (string.IsNullOrEmpty(version))
            return false;
        foreach (string part in version.Split('.'))
        {
            if (!int.TryParse(part, out int num) || num < 0)
                return false;
        }
        return true;
    }

    private static int[] ParseAll(string version)
    {
        string[] parts = version.Split('.');
        var nums = new int[parts.Length];
        for (int i = 0; i < parts.Length; i++)
        {
            if (!int.TryParse(parts[i], out nums[i]) || nums[i] < 0)
                throw new FormatException($"'{version}' is not a dotted-numeric version");
        }
        return nums;
    }
}
