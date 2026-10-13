using System;
using System.Collections.Generic;
using System.Threading;
using Godot;
using godottemplate.Core.Loading;
using godottemplate.Server;
using godottemplate.Session;
using godottemplate.UI;

namespace godottemplate.Demo;

/// <summary>
/// Controller for <c>scenes/demo/destination.tscn</c>, the arriving half of the scene-handoff
/// demo (<c>CLAUDE.md</c>, <i>Scene handoff</i>). It arrives under the <see cref="Transition"/>'s
/// loading screen, reads the launch from <see cref="GameSession"/>, runs a placeholder job on an
/// <see cref="AsyncLoader{TIn, TOut}"/> (a seeded wait producing a string), then fades in and
/// frees the controls on <see cref="Transition.Finished"/>. Return records a result on the
/// session and goes back to the hub the same way. A direct boot (no launch in flight) launches
/// itself with a fresh seed and puts its own loading screen up, so it arrives as it would from
/// the hub.
/// </summary>
public partial class DestinationScreen : Node2D, IAgentDemo
{
    /// <summary>This screen's destination id on a <see cref="SessionLaunch"/>.</summary>
    public const string Id = "destination";

    public const string LoadingText = "Travelling";
    public const string ReturnLoadingText = "Returning";

    private readonly AsyncLoader<ulong, string> _loader = new(Prepare, GD.PushWarning, GD.PushError);
    private MenuOverlay _menu;
    private Button _return;
    private Label _payload;
    private ulong _seed;
    private bool _loaded;
    private bool _arrived;
    private bool _returning;
    private bool _fadeHooked;

    public override void _Ready()
    {
        AddToGroup(HandoffState.AgentGroup);
        _menu = GetNode<MenuOverlay>("%MenuOverlay");
        _return = GetNode<Button>("%ReturnButton");
        _payload = GetNode<Label>("%Payload");
        GetNode<TopBar>("%TopBar").MenuPressed += _menu.Open;
        _menu.QuitPressed += () => GetTree().Quit();
        _return.Pressed += OnReturnPressed;

        var launch = GameSession.Launch;
        if (launch == null)
        {
            GD.PushWarning("[Destination] booted with no launch on the session carrier (a direct boot): launching itself on a fresh seed");
            launch = GameSession.BeginLaunch(Id);
        }
        _seed = launch.Seed;

        // A direct boot has no loading screen yet: put one up, so the scene arrives as it would from the hub.
        var transition = Transition.Instance;
        if (transition != null && transition.Stage == TransitionStage.None) transition.Hold(LoadingText);

        _loader.Start(_seed);
        ApplyLock();
    }

    public override void _ExitTree()
    {
        if (_fadeHooked && Transition.Instance != null) Transition.Instance.Finished -= OnFadedIn;
        _fadeHooked = false;
    }

    public override void _Process(double delta)
    {
        if (!_loaded && _loader.Poll())
        {
            _loaded = true;
            _payload.Text = _loader.Result;
            Arrive();
        }
        if (Input.IsActionJustPressed("ui_cancel"))
        {
            if (_menu.IsOpen) _menu.Close();
            else _menu.Open();
        }
        ApplyLock();
    }

    /// <summary>
    /// The placeholder job (worker thread, engine-free): a few seeded "attempts", all but the last
    /// failing with a warning, each a short wait, then a string. A real game generates its level,
    /// builds its roster, decodes its save here.
    /// </summary>
    private static string Prepare(ulong seed, ILoadProgress progress)
    {
        var rng = new Random(unchecked((int)(seed ^ (seed >> 32))));
        int tries = 1 + (int)(seed % 3);
        for (int i = 0; i < tries; i++)
        {
            progress.Attempt();
            Thread.Sleep(rng.Next(100, 200));
            if (i < tries - 1) progress.Fail($"destination {seed}: attempt {i} came up empty (placeholder)");
        }
        return $"Payload {seed % 100000:D5}";
    }

    /// <summary>Everything is placed: fade in (the overlay waits out its minimum), or arrive at once with no loading screen up.</summary>
    private void Arrive()
    {
        var transition = Transition.Instance;
        if (transition == null || transition.Stage != TransitionStage.Hold)
        {
            OnFadedIn();
            return;
        }
        transition.Finished += OnFadedIn;
        _fadeHooked = true;
        transition.FadeIn();
    }

    private void OnFadedIn()
    {
        if (_fadeHooked && Transition.Instance != null) Transition.Instance.Finished -= OnFadedIn;
        _fadeHooked = false;
        _arrived = true;
    }

    private void OnReturnPressed()
    {
        if (ControlsLock != null) return;
        _returning = true;
        ApplyLock();
        GameSession.Finish(new SessionResult("complete", _seed));
        var transition = Transition.Instance;
        void Leave() => GetTree().ChangeSceneToFile(GameScenes.MainMenu);
        if (transition == null || !transition.BeginOut(() =>
            {
                transition.Hold(ReturnLoadingText);
                Leave();
            }))
            Leave();
    }

    /// <summary>Why the player's controls are locked, or null when they are free.</summary>
    private string ControlsLock =>
        !_loaded ? "loading"
        : _returning || !_arrived || (Transition.Instance?.Stage ?? TransitionStage.None) != TransitionStage.None ? "transition"
        : _menu.IsOpen ? "menu"
        : null;

    private void ApplyLock() => _return.Disabled = ControlsLock != null;

    // ---- Agent surface ------------------------------------------------------------------

    public Dictionary<string, object> GetDemoState() => HandoffState.Build(Id, ControlsLock, new Dictionary<string, object>
    {
        ["seed"] = _seed.ToString(),
        ["payload"] = _loaded ? _loader.Result : null,
        ["returning"] = _returning,
        ["arrived"] = _arrived,
        ["loader"] = new Dictionary<string, object>
        {
            ["stage"] = _loader.Stage.ToString().ToLowerInvariant(),
            ["attempts"] = _loader.Attempts,
            ["failures"] = _loader.Failures,
            ["elapsed_ms"] = (long)_loader.Elapsed.TotalMilliseconds,
            ["error"] = _loader.Error,
        },
    });
}
