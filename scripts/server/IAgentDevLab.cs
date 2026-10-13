using System.Collections.Generic;

namespace godottemplate.Server;

/// <summary>
/// Typed agent surface for the dev-only lab (the <see cref="IAgentExample"/> pattern), and the
/// shipped example of a <b>dev-only scene's</b> surface (<c>CLAUDE.md</c>, <i>Dev-only scenes</i>).
/// The lab's controller (<c>scripts/devtools/DevLab.cs</c>) implements it and joins the
/// <c>agent_devlab</c> group, so <c>/devlab/*</c> answers only while
/// <c>scenes/devtools/dev_lab.tscn</c> is the current scene — booted by path, never reached from
/// the game — and otherwise 503s with a message naming the scene to boot.
/// </summary>
public interface IAgentDevLab
{
    /// <summary>
    /// The lab's seed (a string: 64-bit), where it came from (<c>env</c>, <c>default</c> or
    /// <c>agent</c>), how many values have been drawn since it was set, and the last one (null
    /// before the first draw).
    /// </summary>
    Dictionary<string, object> GetDevLabState();

    /// <summary>Draw the next value from the seeded generator (the Reroll button does the same).</summary>
    (bool ok, string error) Reroll();

    /// <summary>Reseed the generator: the draw count resets, so the same seed replays the same draws.</summary>
    (bool ok, string error) SetSeed(ulong seed);
}
