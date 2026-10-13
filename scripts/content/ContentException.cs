using System;
using Godot;
using godottemplate.Core.Content;

namespace godottemplate.Content;

/// <summary>
/// The Godot-side content error: raised when a JSON content definition file is missing, malformed, or references an
/// unknown id/enum. Constructing one also emits a <see cref="GD.PushError"/> so the
/// failure is visible in the Godot console as a clean single line before it propagates
/// (a thrown content error is a hard, loud failure — never a silently empty world).
/// Engine-free parsing throws <see cref="ContentError"/> instead; <see cref="From"/>
/// bridges one across so it is logged the same way.
/// </summary>
public sealed class ContentException : Exception
{
    private ContentException(string message) : base(message) { }

    /// <summary>A file-level error (missing file, malformed JSON, wrong top-level shape).</summary>
    public static ContentException For(string file, string message)
        => Build($"[Content] {file}: {message}");

    /// <summary>An entry-level error, naming the offending definition.</summary>
    public static ContentException For(string file, string context, string message)
        => Build($"[Content] {file} ({context}): {message}");

    /// <summary>Wrap an engine-free <see cref="ContentError"/> (already formatted) so it logs like any other content error.</summary>
    public static ContentException From(ContentError error)
        => Build(error.Message);

    private static ContentException Build(string full)
    {
        GD.PushError(full);
        return new ContentException(full);
    }
}
