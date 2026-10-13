using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace godottemplate.Core.Loading;

/// <summary>Where an <see cref="AsyncLoader{TIn, TOut}"/> stands. A faulted load has no stage of its own: it stays <see cref="Loading"/> with <c>Error</c> set.</summary>
public enum LoadStage
{
    /// <summary>Nothing started (or the last result was cleared).</summary>
    Idle,
    /// <summary>The worker is running, or it threw (<c>Error</c> is set).</summary>
    Loading,
    /// <summary><c>Result</c> is ready.</summary>
    Done,
}

/// <summary>
/// What a load's work reports while it runs on the worker thread: attempts and failures are
/// counted at once (readable from the main thread mid-load), warnings are queued and delivered
/// on the main thread by the next <see cref="AsyncLoader{TIn, TOut}.Poll"/>.
/// </summary>
public interface ILoadProgress
{
    /// <summary>An attempt started (a retrying job calls it once per try).</summary>
    void Attempt();

    /// <summary>An attempt failed: counted, and <paramref name="warning"/> queued for the main thread.</summary>
    void Fail(string warning);

    /// <summary>Queue a warning for the main thread without counting a failure.</summary>
    void Warn(string warning);
}

/// <summary>
/// A generic background load: the caller gathers <typeparamref name="TIn"/> on the main thread
/// (every engine read happens there, so the work touches nothing engine-bound), <see cref="Start"/>
/// runs the work on <see cref="Task.Run(Action)"/>, and the owning scene calls <see cref="Poll"/>
/// each frame, which delivers the queued warnings and, once the worker is through, publishes
/// <see cref="Result"/>. Nothing marshals back by callback, so a scene that is freed mid-load
/// simply stops polling.
/// </summary>
/// <remarks>
/// Engine-free: the log sinks are injected (a Godot caller passes <c>GD.PushWarning</c> /
/// <c>GD.PushError</c>), so the whole loader runs under the unit tests. A work function that
/// throws leaves the stage at <see cref="LoadStage.Loading"/> with <see cref="Error"/> set and
/// logged once; a new <see cref="Start"/> may then retry. <see cref="Elapsed"/> runs from
/// <see cref="Start"/> until the <see cref="Poll"/> that sees the worker finished.
/// </remarks>
public sealed class AsyncLoader<TIn, TOut>
{
    private readonly Func<TIn, ILoadProgress, TOut> _work;
    private readonly Action<string> _warn;
    private readonly Action<string> _error;
    private readonly ConcurrentQueue<string> _warnings = new();
    private readonly Stopwatch _clock = new();
    private readonly Progress _progress;
    private Task<TOut> _task;
    private int _attempts;
    private int _failures;
    private bool _errorLogged;

    /// <param name="work">The job, run on a worker thread: engine-free code only.</param>
    /// <param name="warn">Main-thread sink for the queued warnings (null drops them).</param>
    /// <param name="error">Main-thread sink for a faulted job's error, called once (null drops it).</param>
    public AsyncLoader(Func<TIn, ILoadProgress, TOut> work, Action<string> warn = null, Action<string> error = null)
    {
        _work = work ?? throw new ArgumentNullException(nameof(work));
        _warn = warn;
        _error = error;
        _progress = new Progress(this);
    }

    public LoadStage Stage { get; private set; } = LoadStage.Idle;

    /// <summary>The input of the load under way or done (default before the first <see cref="Start"/>).</summary>
    public TIn Input { get; private set; }

    /// <summary>Attempts the work reported so far (<see cref="ILoadProgress.Attempt"/>).</summary>
    public int Attempts => Volatile.Read(ref _attempts);

    /// <summary>Attempts the work reported failed (<see cref="ILoadProgress.Fail"/>).</summary>
    public int Failures => Volatile.Read(ref _failures);

    /// <summary>The faulted work's exception message, or null.</summary>
    public string Error { get; private set; }

    /// <summary>Wall-clock time since <see cref="Start"/>, frozen once a <see cref="Poll"/> sees the worker finished.</summary>
    public TimeSpan Elapsed => _clock.Elapsed;

    /// <summary>The finished load's output (default until <see cref="Done"/>).</summary>
    public TOut Result { get; private set; }

    public bool Done => Stage == LoadStage.Done;

    /// <summary>Whether a worker is running right now (not yet seen finished, not faulted).</summary>
    public bool Running => Stage == LoadStage.Loading && Error == null;

    /// <summary>Start loading <paramref name="input"/> (main thread). Returns why it couldn't start (a load already running), or null.</summary>
    public string Start(TIn input)
    {
        if (Running) return "a load is already running";
        Input = input;
        Result = default;
        Error = null;
        _errorLogged = false;
        _attempts = _failures = 0;
        while (_warnings.TryDequeue(out _)) { }
        Stage = LoadStage.Loading;
        _clock.Restart();
        _task = Task.Run(() => _work(input, _progress));
        return null;
    }

    /// <summary>
    /// Call each frame (main thread): delivers the queued warnings and, once the worker is
    /// through, publishes its result (or records its error). Returns <see cref="Done"/>.
    /// </summary>
    public bool Poll()
    {
        while (_warnings.TryDequeue(out var warning)) _warn?.Invoke(warning);
        if (Stage != LoadStage.Loading || _task == null || !_task.IsCompleted) return Done;
        if (_task.IsFaulted || _task.IsCanceled)
        {
            Error ??= _task.Exception?.GetBaseException().Message ?? "the load failed";
            if (!_errorLogged) _error?.Invoke($"load failed: {Error}");
            _errorLogged = true;
            _clock.Stop();
            return false;
        }
        Result = _task.Result;
        _task = null;
        _clock.Stop();
        Stage = LoadStage.Done;
        return true;
    }

    /// <summary>Forget a finished (or faulted) load; a running one is left alone.</summary>
    public void Clear()
    {
        if (Running) return;
        _task = null;
        Error = null;
        Stage = LoadStage.Idle;
        Result = default;
    }

    private sealed class Progress : ILoadProgress
    {
        private readonly AsyncLoader<TIn, TOut> _owner;

        public Progress(AsyncLoader<TIn, TOut> owner) => _owner = owner;

        public void Attempt() => Interlocked.Increment(ref _owner._attempts);

        public void Fail(string warning)
        {
            Interlocked.Increment(ref _owner._failures);
            if (warning != null) _owner._warnings.Enqueue(warning);
        }

        public void Warn(string warning)
        {
            if (warning != null) _owner._warnings.Enqueue(warning);
        }
    }
}
