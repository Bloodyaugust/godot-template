using System;
using System.Collections.Generic;
using godottemplate.Core.Validation;
using Xunit;

namespace godottemplate.Tests.Validation;

public class BuildResultTests
{
    private sealed record Widget(string Name);

    private static BuildResult<Widget> Build(string name, int size)
    {
        var issues = new List<BuildIssue>();
        if (string.IsNullOrEmpty(name)) issues.Add(new BuildIssue("missing_name", "widget", "a widget needs a name"));
        if (size <= 0) issues.Add(new BuildIssue("bad_size", "widget / size", $"size {size} must be positive"));
        return BuildResult<Widget>.Of(issues, () => new Widget(name));
    }

    [Fact]
    public void CleanBuildSucceedsWithNoIssues()
    {
        var result = Build("w", 1);
        Assert.True(result.Ok);
        Assert.Equal("w", result.Value.Name);
        Assert.Empty(result.Issues);
    }

    [Fact]
    public void FailureCollectsEveryIssueInOrder()
    {
        var result = Build("", 0);
        Assert.False(result.Ok);
        Assert.Null(result.Value);
        Assert.Equal(new[] { "missing_name", "bad_size" }, Array.ConvertAll(new List<BuildIssue>(result.Issues).ToArray(), i => i.Code));
        Assert.Equal("bad_size at widget / size: size 0 must be positive", result.Issues[1].ToString());
    }

    [Fact]
    public void FailureWithoutIssuesIsRefused()
        => Assert.Throws<ArgumentException>(() => BuildResult<Widget>.Failure(new List<BuildIssue>()));

    [Fact]
    public void SuccessWithoutValueIsRefused()
        => Assert.Throws<ArgumentNullException>(() => BuildResult<Widget>.Success(null));
}
