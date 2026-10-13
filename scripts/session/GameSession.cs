using System;
using System.Collections.Generic;
using Godot;
using godottemplate.Server;

namespace godottemplate.Session;

// PLACEHOLDER RECORDS — the thing a real game replaces (like IAgentExample). A launch says where
// the player is going and with which seed; a result says how it went. Swap them for the game's
// own (a mission and loadout, a match mode, a result kind enum with banner texts) and keep the
// carrier's shape.

/// <summary>A launch going out of one screen to another: where to, and the seed the arriving scene loads with.</summary>
public sealed record SessionLaunch(string Destination, ulong Seed);

/// <summary>A result coming back: its kind (a snake_case name agents see) and the launch's seed.</summary>
public sealed record SessionResult(string Kind, ulong Seed);

/// <summary>
/// The session carrier, the transient leg of the CLAUDE.md cross-scene trio: what one screen
/// hands the next across a <c>ChangeSceneToFile</c>, which can't pass arguments. The leaving
/// screen writes the <see cref="Launch"/> (<see cref="BeginLaunch"/>); the arriving one reads it,
/// and writes the <see cref="Result"/> coming back (<see cref="Finish"/>), which the first reads
/// on return. Process-lifetime and never persisted. The shipped users are the scene-handoff demo
/// pair (<c>scripts/ui/MainScreen.cs</c> ↔ <c>scripts/demo/DestinationScreen.cs</c>).
/// </summary>
/// <remarks>
/// Registered as an autoload so it lives for the process beside the other cross-scene services
/// and can answer <c>/session/state</c> (group <c>agent_session</c>), but the state is static:
/// callers use <c>GameSession.Launch</c> and friends directly, order-free, like
/// <see cref="PlayerProfile"/>. It has no per-frame behaviour.
/// </remarks>
public partial class GameSession : Node, IAgentSession
{
    public const string AgentGroup = "agent_session";

    /// <summary>The launch in flight (set by the leaving screen, read by the arriving one), or null.</summary>
    public static SessionLaunch Launch { get; private set; }

    /// <summary>The last launch's result (set when it finishes, read by the screen returned to), or null.</summary>
    public static SessionResult Result { get; private set; }

    /// <summary>The seed an agent asked the next launch to use, or null for a fresh one. <see cref="BeginLaunch"/> consumes it.</summary>
    public static ulong? NextSeed { get; set; }

    public override void _Ready() => AddToGroup(AgentGroup);

    /// <summary>
    /// Start a launch to <paramref name="destination"/> with the next seed (the agent's
    /// <see cref="NextSeed"/> if set, which it consumes, else a fresh random one), clearing the
    /// previous result.
    /// </summary>
    public static SessionLaunch BeginLaunch(string destination)
    {
        var seed = NextSeed ?? FreshSeed();
        NextSeed = null;
        Launch = new SessionLaunch(destination, seed);
        Result = null;
        return Launch;
    }

    /// <summary>End the launch in flight with a result; the launch is cleared.</summary>
    public static void Finish(SessionResult result)
    {
        Result = result;
        Launch = null;
    }

    /// <summary>Forget the last result (once the screen returned to has shown it, if it wants to).</summary>
    public static void ClearResult() => Result = null;

    /// <summary>A fresh 64-bit seed.</summary>
    public static ulong FreshSeed()
    {
        Span<byte> bytes = stackalloc byte[8];
        Random.Shared.NextBytes(bytes);
        return BitConverter.ToUInt64(bytes);
    }

    /// <summary>
    /// The carrier for agent states: the launch, the result and the next seed. <b>Seeds are
    /// strings</b>: a 64-bit seed doesn't survive a JSON number (clients parse doubles, exact only
    /// to 2^53), so it is serialised as its decimal text, and a route taking one parses a string.
    /// </summary>
    public static Dictionary<string, object> GetState() => new()
    {
        ["launch"] = Launch == null ? null : new Dictionary<string, object>
        {
            ["destination"] = Launch.Destination,
            ["seed"] = Launch.Seed.ToString(),
        },
        ["result"] = Result == null ? null : new Dictionary<string, object>
        {
            ["kind"] = Result.Kind,
            ["seed"] = Result.Seed.ToString(),
        },
        ["next_seed"] = NextSeed?.ToString(),
    };

    public Dictionary<string, object> GetSessionState() => GetState();
}
