using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using godottemplate.Core.Loading;
using Xunit;

namespace godottemplate.Tests.Loading;

/// <summary>
/// <c>AsyncLoader&lt;TIn, TOut&gt;</c> (<c>scripts/core/loading/</c>): work off the main thread,
/// results and warnings published only by <c>Poll</c>, attempts and failures counted, a faulted
/// job's error logged once. Pins the code-documented contract — the loader has no design doc;
/// its intent lives in <c>CLAUDE.md</c>, <i>Scene handoff</i>.
/// </summary>
public class AsyncLoaderTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    private static bool PollUntil<TIn, TOut>(AsyncLoader<TIn, TOut> loader, Func<bool> until)
    {
        var clock = Stopwatch.StartNew();
        while (clock.Elapsed < Timeout)
        {
            loader.Poll();
            if (until()) return true;
            Thread.Sleep(5);
        }
        return false;
    }

    [Fact]
    public void IdleUntilStartedAndPollReportsNotDone()
    {
        var loader = new AsyncLoader<int, string>((n, _) => n.ToString());
        Assert.Equal(LoadStage.Idle, loader.Stage);
        Assert.False(loader.Poll());
        Assert.Null(loader.Result);
    }

    [Fact]
    public void ResultIsPublishedByPollNotByTheWorker()
    {
        using var finished = new ManualResetEventSlim();
        var loader = new AsyncLoader<int, string>((n, _) =>
        {
            finished.Set();
            return $"value {n}";
        });
        Assert.Null(loader.Start(7));
        Assert.Equal(LoadStage.Loading, loader.Stage);
        Assert.True(finished.Wait(Timeout));
        Assert.Null(loader.Result);                 // not yet polled
        Assert.True(PollUntil(loader, () => loader.Done));
        Assert.Equal("value 7", loader.Result);
        Assert.Equal(7, loader.Input);
        Assert.Null(loader.Error);
    }

    [Fact]
    public void AStartWhileRunningIsRefused()
    {
        using var gate = new ManualResetEventSlim();
        var loader = new AsyncLoader<int, int>((n, _) =>
        {
            gate.Wait(Timeout);
            return n;
        });
        Assert.Null(loader.Start(1));
        Assert.True(loader.Running);
        Assert.Equal("a load is already running", loader.Start(2));
        gate.Set();
        Assert.True(PollUntil(loader, () => loader.Done));
        Assert.Equal(1, loader.Result);
        Assert.Null(loader.Start(3));               // done: a new load may start
        Assert.True(PollUntil(loader, () => loader.Done));
        Assert.Equal(3, loader.Result);
    }

    [Fact]
    public void AttemptsAndFailuresAreCountedAndWarningsDeliveredInOrderOnPoll()
    {
        var warnings = new List<string>();
        var loader = new AsyncLoader<int, string>((tries, progress) =>
        {
            for (int i = 0; i < tries - 1; i++)
            {
                progress.Attempt();
                progress.Fail($"attempt {i} failed");
            }
            progress.Attempt();
            progress.Warn("last try");
            return "ok";
        }, warnings.Add);
        loader.Start(3);
        Assert.True(PollUntil(loader, () => loader.Done));
        Assert.Equal(3, loader.Attempts);
        Assert.Equal(2, loader.Failures);
        Assert.Equal(new[] { "attempt 0 failed", "attempt 1 failed", "last try" }, warnings);
    }

    [Fact]
    public void AFaultedJobStaysLoadingWithItsErrorLoggedOnceAndMayRetry()
    {
        var errors = new List<string>();
        var loader = new AsyncLoader<bool, string>((fail, _) =>
            fail ? throw new InvalidOperationException("broken content") : "fine", error: errors.Add);
        loader.Start(true);
        Assert.True(PollUntil(loader, () => loader.Error != null));
        loader.Poll();
        loader.Poll();
        Assert.Equal(LoadStage.Loading, loader.Stage);
        Assert.False(loader.Running);
        Assert.Equal("broken content", loader.Error);
        Assert.Single(errors);
        Assert.Contains("broken content", errors[0]);

        Assert.Null(loader.Start(false));
        Assert.Null(loader.Error);
        Assert.True(PollUntil(loader, () => loader.Done));
        Assert.Equal("fine", loader.Result);
    }

    [Fact]
    public void ElapsedFreezesOnceDoneAndClearReturnsToIdle()
    {
        var loader = new AsyncLoader<int, int>((n, _) =>
        {
            Thread.Sleep(20);
            return n;
        });
        loader.Start(5);
        Assert.True(PollUntil(loader, () => loader.Done));
        var elapsed = loader.Elapsed;
        Assert.True(elapsed >= TimeSpan.FromMilliseconds(15));
        Thread.Sleep(20);
        Assert.Equal(elapsed, loader.Elapsed);
        loader.Clear();
        Assert.Equal(LoadStage.Idle, loader.Stage);
        Assert.Equal(0, loader.Result);
    }
}
