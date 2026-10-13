using System.Collections.Generic;
using Godot;
using godottemplate.Camera;
using godottemplate.Core.Snapshots;
using godottemplate.Demo;
using godottemplate.Server;
using godottemplate.Session;
using godottemplate.Test;

namespace godottemplate.UI;

/// <summary>
/// Controller for the template's main scene (<c>scenes/main.tscn</c>), the hub of the
/// scene-handoff demo (<c>CLAUDE.md</c>, <i>Scene handoff</i>). Wires the chrome — the
/// <see cref="TopBar"/>'s Menu button and Esc open the <see cref="MenuOverlay"/>, Esc closes it
/// again, and the overlay's Quit asks through the <see cref="ConfirmDialog"/> (<c>main.quit.confirm</c>
/// / <c>main.quit.cancel</c>) — and the Travel button: lock the controls, launch on
/// <see cref="GameSession"/>, fade out, hold the loading screen and change to the destination.
/// Arriving back under a held loading screen it fades in and frees the controls on
/// <see cref="Transition.Finished"/>. The scene's structure lives in the <c>.tscn</c>.
/// </summary>
public partial class MainScreen : Node2D, IAgentDemo
{
    public const string SceneName = "hub";

    private MenuOverlay _menu;
    private ConfirmDialog _quit;
    private Button _travel;
    private Player _player;
    private ZoomCamera _camera;
    private bool _travelling;
    private bool _arriving;

    public override void _Ready()
    {
        AddToGroup(HandoffState.AgentGroup);
        _menu = GetNode<MenuOverlay>("%MenuOverlay");
        _quit = GetNode<ConfirmDialog>("%QuitDialog");
        _travel = GetNode<Button>("%TravelButton");
        _player = GetNode<Player>("%Player");
        _camera = GetNode<ZoomCamera>("%Camera");
        GetNode<TopBar>("%TopBar").MenuPressed += _menu.Open;
        _menu.QuitPressed += () => HandoffState.AskQuit(_quit, "main");
        _quit.Chosen += index =>
        {
            if (index == HandoffState.QuitConfirm) GetTree().Quit();
        };
        _travel.Pressed += OnTravelPressed;

        // Back from the destination: the loading screen is still held over us.
        var transition = Transition.Instance;
        if (transition != null && transition.Stage == TransitionStage.Hold)
        {
            _arriving = true;
            transition.Finished += OnFadedIn;
            transition.FadeIn();
        }
        ApplyLock();
    }

    public override void _ExitTree()
    {
        if (_arriving && Transition.Instance != null) Transition.Instance.Finished -= OnFadedIn;
    }

    // Polled rather than taken from _UnhandledInput so the agent /input/action route drives
    // Esc exactly like a real key press.
    public override void _Process(double delta)
    {
        // The quit dialog takes Esc itself while it is up.
        if (!_quit.IsOpen && Input.IsActionJustPressed("ui_cancel"))
        {
            if (_menu.IsOpen) _menu.Close();
            else _menu.Open();
        }
        ApplyLock();
    }

    private void OnFadedIn()
    {
        if (Transition.Instance != null) Transition.Instance.Finished -= OnFadedIn;
        _arriving = false;
    }

    private void OnTravelPressed()
    {
        var transition = Transition.Instance;
        if (ControlsLock != null || transition == null) return;
        _travelling = true;
        ApplyLock();
        GameSession.BeginLaunch(DestinationScreen.Id);
        transition.BeginOut(() =>
        {
            transition.Hold(DestinationScreen.LoadingText);
            GetTree().ChangeSceneToFile(GameScenes.Destination);
        });
    }

    /// <summary>Why the player's controls are locked, or null when they are free.</summary>
    private string ControlsLock =>
        _travelling || _arriving || (Transition.Instance?.Stage ?? TransitionStage.None) != TransitionStage.None ? "transition"
        : _menu.IsOpen || _quit.IsOpen ? "menu"
        : null;

    private void ApplyLock()
    {
        bool locked = ControlsLock != null;
        _player.Locked = locked;
        _camera.ZoomInputEnabled = !locked;
        _travel.Disabled = locked;
    }

    public Dictionary<string, object> GetDemoState() => HandoffState.Build(SceneName, ControlsLock, _quit, Snapshot.Of(
        ("travelling", _travelling),
        ("arriving", _arriving),
        ("floating_labels", _player.LiveLabels)));
}
