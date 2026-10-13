using System;
using System.Collections.Generic;
using Godot;
using godottemplate.Server;

namespace godottemplate.UI;

/// <summary>
/// The transition overlay: an autoload <see cref="CanvasLayer"/> (<c>scenes/ui/transition.tscn</c>)
/// that outlives scene swaps, so one screen can fade out, hold a loading screen while the next
/// scene loads under it, and fade into that scene. It owns a flat background and the loading
/// text at the bottom with its cycling ellipsis. The flow it serves: <c>CLAUDE.md</c>,
/// <i>Scene handoff</i>.
/// </summary>
/// <remarks>
/// <para><b>Stages.</b> <see cref="TransitionStage.None"/> → <see cref="BeginOut"/> →
/// <see cref="TransitionStage.Out"/> (fading to opaque over <see cref="OutSeconds"/>, then the
/// caller's callback) → <see cref="Hold"/> → <see cref="TransitionStage.Hold"/> (opaque: the
/// loading screen; the scene swap happens under it) → <see cref="FadeIn"/> →
/// <see cref="TransitionStage.In"/> (fading out over <see cref="InSeconds"/>) →
/// <see cref="Finished"/>, back to None. <see cref="Hold"/> from None skips the fade out (a
/// directly booted scene putting its own loading screen up).</para>
/// <para><b>The hold minimum is the overlay's job.</b> The arriving scene calls
/// <see cref="FadeIn"/> as soon as it is ready; if the minimum hasn't passed the fade is queued
/// (<c>fade_pending</c>) and starts the moment it does. <see cref="SkipMinHold"/> ends the
/// minimum early (an agent's skip).</para>
/// <para><b>Layering.</b> The layer is <see cref="OverlayLayer"/>: above every scene's world
/// and plain UI, below each scene's chrome, which sits on a CanvasLayer of at least
/// <see cref="ChromeLayer"/> so the top bar stays up through the transition.</para>
/// <para><b>Input.</b> While the overlay is up, a full-rect Control stops the mouse, so clicks
/// never reach the world or a lower layer's UI (the chrome above still gets them). It does not
/// block the keyboard or polled <c>Input.IsActionPressed</c>: the scenes lock their own controls
/// for the transition (the controls-lock reason pattern).</para>
/// </remarks>
public partial class Transition : CanvasLayer, IAgentTransition
{
    public const string AgentGroup = "agent_transition";

    /// <summary>The overlay's CanvasLayer index: above the world, below every scene's chrome.</summary>
    public const int OverlayLayer = 10;

    /// <summary>The lowest CanvasLayer index a scene's chrome may use to draw over the overlay.</summary>
    public const int ChromeLayer = 20;

    /// <summary>How long the fade to opaque takes.</summary>
    public const double OutSeconds = 0.5;

    /// <summary>The loading screen's default minimum length.</summary>
    public const double MinHoldSeconds = 2;

    /// <summary>How long the fade from the loading screen into the arriving scene takes.</summary>
    public const double InSeconds = 0.5;

    /// <summary>How long each step of the loading text's ellipsis shows.</summary>
    public const double EllipsisStepSeconds = 0.4;

    /// <summary>The ellipsis' dot counts, looped: one, two, three, two, (one, …).</summary>
    private static readonly int[] EllipsisDots = { 1, 2, 3, 2 };

    /// <summary>The running instance; set in <see cref="_Ready"/>, never null after autoload.</summary>
    public static Transition Instance { get; private set; }

    /// <summary>The current stage.</summary>
    public TransitionStage Stage { get; private set; } = TransitionStage.None;

    /// <summary>The loading text without its ellipsis.</summary>
    public string LoadingText { get; private set; } = "";

    /// <summary>Seconds in <see cref="TransitionStage.Hold"/> so far (0 outside it).</summary>
    public double HeldSeconds { get; private set; }

    /// <summary>The current hold's minimum (<see cref="MinHoldSeconds"/> unless the caller asked otherwise).</summary>
    public double HoldMinimum { get; private set; } = MinHoldSeconds;

    /// <summary>Whether the hold's minimum has passed (or was skipped).</summary>
    public bool MinHoldElapsed => Stage == TransitionStage.Hold && (_minSkipped || HeldSeconds >= HoldMinimum);

    /// <summary>Whether a <see cref="FadeIn"/> is waiting for the hold minimum.</summary>
    public bool FadePending { get; private set; }

    /// <summary>The transition ran to the end (the fade-in completed); the overlay is gone.</summary>
    public event Action Finished;

    private Control _root;
    private Control _loading;
    private RichTextLabel _text;
    private Tween _fade;
    private bool _minSkipped;
    private double _ellipsisClock;

    public override void _Ready()
    {
        Instance = this;
        Layer = OverlayLayer;
        ProcessMode = ProcessModeEnum.Always;
        AddToGroup(AgentGroup);
        _root = GetNode<Control>("%Root");
        _loading = GetNode<Control>("%Loading");
        _text = GetNode<RichTextLabel>("%Text");
        Reset();
    }

    public override void _ExitTree()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// Start a transition out: the overlay fades to opaque over <see cref="OutSeconds"/>, blocking
    /// the mouse at once, then <paramref name="opaque"/> runs — typically <see cref="Hold"/> and a
    /// <c>ChangeSceneToFile</c>. Ignored (false) while another transition runs.
    /// </summary>
    public bool BeginOut(Action opaque = null)
    {
        if (Stage != TransitionStage.None)
        {
            GD.PushWarning($"[Transition] BeginOut from {StageName}; expected none");
            return false;
        }
        Reset();
        Stage = TransitionStage.Out;
        _root.Visible = true;
        _root.MouseFilter = Control.MouseFilterEnum.Stop;
        _root.Modulate = new Color(1, 1, 1, 0);
        _fade = CreateTween();
        _fade.TweenProperty(_root, "modulate:a", 1f, OutSeconds);
        _fade.Finished += () =>
        {
            _fade = null;
            opaque?.Invoke();
        };
        return true;
    }

    /// <summary>
    /// Show the loading screen: opaque, with <paramref name="text"/> and its cycling ellipsis.
    /// Called when the fade out ends (or from None, by a directly booted scene); the caller may
    /// change scenes right after. <paramref name="minimum"/> overrides the hold's minimum.
    /// </summary>
    public void Hold(string text, double minimum = MinHoldSeconds)
    {
        if (Stage is not (TransitionStage.Out or TransitionStage.None))
            GD.PushWarning($"[Transition] Hold from {StageName}; expected out or none");
        _fade?.Kill();
        _fade = null;
        Stage = TransitionStage.Hold;
        LoadingText = StripEllipsis(text);
        HoldMinimum = minimum;
        HeldSeconds = 0;
        FadePending = false;
        _minSkipped = false;
        _root.Visible = true;
        _root.Modulate = Colors.White;
        _root.MouseFilter = Control.MouseFilterEnum.Stop;
        _loading.Visible = true;
        _ellipsisClock = 0;
        ShowText();
    }

    /// <summary>
    /// Fade the loading screen out into the arriving scene over <see cref="InSeconds"/>. Call it
    /// once the scene is ready; if the hold minimum hasn't passed, the fade waits for it.
    /// </summary>
    public void FadeIn()
    {
        if (Stage != TransitionStage.Hold)
        {
            GD.PushWarning($"[Transition] FadeIn from {StageName}; expected hold");
            return;
        }
        if (MinHoldElapsed) StartFade();
        else FadePending = true;
    }

    /// <summary>End the hold's minimum now (an agent's skip); a pending fade starts at once.</summary>
    public void SkipMinHold()
    {
        if (Stage != TransitionStage.Hold) return;
        _minSkipped = true;
        if (FadePending) StartFade();
    }

    /// <summary>Change the loading text mid-hold (the ellipsis keeps cycling).</summary>
    public void SetLoadingText(string text)
    {
        LoadingText = StripEllipsis(text);
        ShowText();
    }

    /// <summary>Drop any transition at once (a scene that must abort it); no <see cref="Finished"/>, no out callback.</summary>
    public void Cancel() => Reset();

    public override void _Process(double delta)
    {
        if (Stage == TransitionStage.Hold)
        {
            HeldSeconds += delta;
            if (FadePending && MinHoldElapsed) StartFade();
        }
        if (Stage is TransitionStage.Hold or TransitionStage.In)
        {
            _ellipsisClock += delta;
            ShowText();
        }
    }

    private string StageName => Stage.ToString().ToLowerInvariant();

    private Vector2 ViewSize => GetViewport().GetVisibleRect().Size;

    private void StartFade()
    {
        FadePending = false;
        Stage = TransitionStage.In;
        _fade?.Kill();
        _fade = CreateTween();
        _fade.TweenProperty(_root, "modulate:a", 0f, InSeconds);
        _fade.Finished += () =>
        {
            _fade = null;
            Reset();
            Finished?.Invoke();
        };
    }

    private void Reset()
    {
        _fade?.Kill();
        _fade = null;
        Stage = TransitionStage.None;
        HeldSeconds = 0;
        HoldMinimum = MinHoldSeconds;
        FadePending = false;
        _minSkipped = false;
        _root.Visible = false;
        _root.Modulate = Colors.White;
        _root.MouseFilter = Control.MouseFilterEnum.Ignore;
        _loading.Visible = false;
    }

    /// <summary>The text as shown, dots included.</summary>
    public string ShownText => LoadingText + new string('.', Dots);

    private int Dots => EllipsisDots[(int)(_ellipsisClock / EllipsisStepSeconds) % EllipsisDots.Length];

    // The missing dots are drawn transparent, so the centred text never shifts as they cycle.
    private void ShowText()
    {
        var hidden = new string('.', 3 - Dots);
        var shown = LoadingText.Replace("[", "[lb]") + new string('.', Dots);
        _text.Text = $"[center][color=#{Palette.Text.ToHtml()}]{shown}[/color][color=#00000000]{hidden}[/color][/center]";
    }

    private static string StripEllipsis(string text) => (text ?? "").TrimEnd('.', '…', ' ');

    // ---- Agent surface ------------------------------------------------------------------

    public Dictionary<string, object> GetTransitionState() => new()
    {
        ["stage"] = StageName,
        ["text"] = Stage is TransitionStage.Hold or TransitionStage.In ? ShownText : LoadingText,
        ["held_seconds"] = HeldSeconds,
        ["min_hold_seconds"] = HoldMinimum,
        ["min_hold_elapsed"] = MinHoldElapsed,
        ["fade_pending"] = FadePending,
        ["view_size"] = new Dictionary<string, object> { ["x"] = ViewSize.X, ["y"] = ViewSize.Y },
        ["blocking_mouse"] = _root.Visible && _root.MouseFilter == Control.MouseFilterEnum.Stop,
        ["layer"] = Layer,
    };

    public (bool ok, string error) Demo(string text, double? seconds)
    {
        if (Stage != TransitionStage.None) return (false, $"a transition is running ({StageName})");
        var hold = seconds ?? MinHoldSeconds;
        if (hold < 0 || hold > 60) return (false, "seconds must be within 0-60");
        Hold(string.IsNullOrEmpty(text) ? "Loading" : text, hold);
        FadeIn();
        return (true, null);
    }

    public (bool ok, string error) SkipHold()
    {
        if (Stage != TransitionStage.Hold) return (false, $"no loading screen is holding (the stage is {StageName})");
        SkipMinHold();
        return (true, null);
    }
}

/// <summary>The transition overlay's stage (see <see cref="Transition"/>).</summary>
public enum TransitionStage
{
    /// <summary>No transition: the overlay is hidden.</summary>
    None,
    /// <summary>Fading to opaque over the leaving scene.</summary>
    Out,
    /// <summary>The loading screen: opaque background and loading text.</summary>
    Hold,
    /// <summary>Fading out into the arriving scene.</summary>
    In,
}
