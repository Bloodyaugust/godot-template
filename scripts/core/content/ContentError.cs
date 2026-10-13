using System;

namespace godottemplate.Core.Content;

/// <summary>
/// Engine-free content-definition failure: a malformed file, a wrong-typed field, an
/// unknown id or enum, or a definition that fails validation. The message is the full
/// single-line form (<c>[Content] file (context): reason</c>) so the Godot side can log it
/// verbatim. Content errors are hard failures — never a silently empty catalog.
/// </summary>
public sealed class ContentError : Exception
{
    public string File { get; }
    public string Context { get; }
    public string Reason { get; }

    public ContentError(string file, string context, string reason)
        : base(context == null ? $"[Content] {file}: {reason}" : $"[Content] {file} ({context}): {reason}")
    {
        File = file;
        Context = context;
        Reason = reason;
    }
}
