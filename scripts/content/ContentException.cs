using System;
using Godot;

namespace godottemplate.Content;

/// <summary>
/// Raised when a JSON content definition file is missing, malformed, or references an
/// unknown id/enum. Constructing one also emits a <see cref="GD.PushError"/> so the
/// failure is visible in the Godot console as a clean single line before it propagates
/// (a thrown content error is a hard, loud failure — never a silently empty world).
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

    private static ContentException Build(string full)
    {
        GD.PushError(full);
        return new ContentException(full);
    }
}
