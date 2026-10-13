using System;
using System.Collections.Generic;

namespace godottemplate.Core.Validation;

/// <summary>
/// One problem found while validating or building something player- or content-authored.
/// <see cref="Code"/> is a stable snake_case identifier (e.g. <c>unknown_module</c>) that UI,
/// tests and agents key on — never parse <see cref="Message"/>. <see cref="Location"/> is a
/// human path to the offending part, outermost first (e.g. <c>slot 'weapon' / node 'barrel_1'</c>).
/// </summary>
/// <remarks>
/// The code is a string rather than an enum so this type stays domain-neutral: each domain
/// declares its codes as <c>const string</c>s in one static class (its closed set), and a
/// generic code parameter would ripple through every signature that passes issues around.
/// The code is the contract; keep a domain's constants stable once UI or tests key on them.
/// </remarks>
public sealed record BuildIssue(string Code, string Location, string Message)
{
    public override string ToString() => $"{Code} at {Location}: {Message}";
}

/// <summary>
/// The outcome of a build: the frozen value, or every issue found. Validation collects all
/// issues instead of stopping at the first (accumulate into a <c>List&lt;BuildIssue&gt;</c>,
/// then return <see cref="Of"/>), so a player sees everything wrong at once.
/// </summary>
public sealed class BuildResult<T> where T : class
{
    private static readonly IReadOnlyList<BuildIssue> NoIssues = Array.Empty<BuildIssue>();

    /// <summary>The built value; null exactly when <see cref="Ok"/> is false.</summary>
    public T Value { get; }

    /// <summary>Every issue found, in discovery order; empty on success.</summary>
    public IReadOnlyList<BuildIssue> Issues { get; }

    public bool Ok => Value != null;

    private BuildResult(T value, IReadOnlyList<BuildIssue> issues)
    {
        Value = value;
        Issues = issues;
    }

    public static BuildResult<T> Success(T value)
        => new(value ?? throw new ArgumentNullException(nameof(value)), NoIssues);

    /// <summary>A failed build. Throws when <paramref name="issues"/> is empty — a failure must say why.</summary>
    public static BuildResult<T> Failure(IReadOnlyList<BuildIssue> issues)
    {
        if (issues == null || issues.Count == 0)
            throw new ArgumentException("a failed build needs at least one issue", nameof(issues));
        return new(null, new List<BuildIssue>(issues));
    }

    /// <summary>
    /// The usual tail of a validator: <see cref="Failure"/> when any issue was collected,
    /// otherwise <see cref="Success"/> with the value <paramref name="build"/> makes (only
    /// called on a clean pass).
    /// </summary>
    public static BuildResult<T> Of(IReadOnlyList<BuildIssue> issues, Func<T> build)
        => issues.Count > 0 ? Failure(issues) : Success(build());
}
