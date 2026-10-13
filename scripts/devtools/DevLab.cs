using System;
using System.Collections.Generic;
using System.Globalization;
using Godot;
using godottemplate.Server;
using godottemplate.UI;

namespace godottemplate.DevTools;

/// <summary>
/// Controller for the dev-only lab, <c>scenes/devtools/dev_lab.tscn</c> — the worked example of
/// a <b>dev-only scene</b> (<c>CLAUDE.md</c>, <i>Dev-only scenes</i>): never reachable from the
/// game (no button leads here; <see cref="GameScenes.DevOnlyLab"/> exists only so the path is one
/// symbol), booted by path with <c>godot-mono --path . res://scenes/devtools/dev_lab.tscn</c>.
/// Its one seeded thing to inspect is a <see cref="Random"/> drawn from by Reroll
/// (<c>devlab.reroll</c>). The seed is <see cref="SeedEnv"/> when that parses as a decimal
/// <c>ulong</c> (invariant culture), else <see cref="DefaultSeed"/> — a value that doesn't parse
/// logs a warning and falls back, never a crash. Agents read and drive it over <c>/devlab/*</c>
/// (<see cref="IAgentDevLab"/>, group <c>agent_devlab</c>). The chrome is the shared top bar
/// (<c>devlab.menu</c>) and menu overlay (<c>devlab.menu.*</c>; Esc toggles it, Quit quits).
/// </summary>
public partial class DevLab : Node2D, IAgentDevLab
{
    public const string AgentGroup = "agent_devlab";

    /// <summary>The env var overriding the boot seed (a decimal uint64).</summary>
    public const string SeedEnv = "GODOT_DEVLAB_SEED";

    /// <summary>The seed when <see cref="SeedEnv"/> is unset or doesn't parse.</summary>
    public const ulong DefaultSeed = 1;

    private MenuOverlay _menu;
    private Label _seedLabel;
    private Label _rollLabel;
    private Button _reroll;

    private ulong _seed;
    private string _seedSource;
    private Random _rng;
    private int _rolls;
    private int? _lastRoll;

    public override void _Ready()
    {
        AddToGroup(AgentGroup);
        _menu = GetNode<MenuOverlay>("%MenuOverlay");
        _seedLabel = GetNode<Label>("%SeedLabel");
        _rollLabel = GetNode<Label>("%RollLabel");
        _reroll = GetNode<Button>("%RerollButton");
        GetNode<TopBar>("%TopBar").MenuPressed += _menu.Open;
        _menu.QuitPressed += () => GetTree().Quit();
        _reroll.Pressed += () => Reroll();

        var (seed, source) = ReadSeed();
        Reseed(seed, source);
    }

    // Polled rather than taken from _UnhandledInput so /input/action drives Esc like a real key.
    public override void _Process(double delta)
    {
        if (Input.IsActionJustPressed("ui_cancel"))
        {
            if (_menu.IsOpen) _menu.Close();
            else _menu.Open();
        }
        _reroll.Disabled = _menu.IsOpen;
    }

    /// <summary>The boot seed and its source: <see cref="SeedEnv"/> when it parses, else the default with a warning.</summary>
    private static (ulong seed, string source) ReadSeed()
    {
        var raw = System.Environment.GetEnvironmentVariable(SeedEnv);
        if (string.IsNullOrWhiteSpace(raw)) return (DefaultSeed, "default");
        if (ulong.TryParse(raw.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var seed))
            return (seed, "env");
        GD.PushWarning($"{SeedEnv}='{raw}' is not a decimal uint64; using the default seed {DefaultSeed}");
        return (DefaultSeed, "default");
    }

    private void Reseed(ulong seed, string source)
    {
        _seed = seed;
        _seedSource = source;
        // System.Random takes an int: fold the 64-bit seed into one, deterministically.
        _rng = new Random(unchecked((int)(seed ^ (seed >> 32))));
        _rolls = 0;
        _lastRoll = null;
        Refresh();
    }

    private void Refresh()
    {
        _seedLabel.Text = $"Seed {_seed.ToString(CultureInfo.InvariantCulture)} ({_seedSource})";
        _rollLabel.Text = _lastRoll is int roll
            ? $"Roll {_rolls}: {roll.ToString(CultureInfo.InvariantCulture)}"
            : "No rolls yet";
    }

    public Dictionary<string, object> GetDevLabState() => new()
    {
        ["seed"] = _seed.ToString(CultureInfo.InvariantCulture),
        ["seed_source"] = _seedSource,
        ["rolls"] = _rolls,
        ["last_roll"] = _lastRoll,
    };

    public (bool ok, string error) Reroll()
    {
        _lastRoll = _rng.Next();
        _rolls++;
        Refresh();
        return (true, null);
    }

    public (bool ok, string error) SetSeed(ulong seed)
    {
        Reseed(seed, "agent");
        return (true, null);
    }
}
