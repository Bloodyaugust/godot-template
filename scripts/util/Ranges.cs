using Godot;

namespace godottemplate.Util;

/// <summary>
/// An inclusive integer <c>[min, max]</c> range that rolls a uniform value. Authored
/// content (JSON definitions under <c>resources/definitions/</c>) can carry ranges
/// instead of concrete values, rolled once per realization so every generated instance
/// is fresh. Parsed from a JSON <c>[min, max]</c> array (or a single number) by
/// <c>DefReader</c>.
/// </summary>
public readonly record struct IntRange(int Min, int Max)
{
    /// <summary>A degenerate range that always rolls <paramref name="value"/>.</summary>
    public static IntRange Single(int value) => new(value, value);

    /// <summary>Roll a uniform value in <c>[Min, Max]</c> (inclusive); a collapsed range returns Min.</summary>
    public int Roll(RandomNumberGenerator rng) => Max <= Min ? Min : rng.RandiRange(Min, Max);
}

/// <summary>
/// An inclusive floating-point <c>[min, max]</c> range that rolls a uniform value —
/// the float counterpart to <see cref="IntRange"/> (distances, angles, delays).
/// </summary>
public readonly record struct FloatRange(float Min, float Max)
{
    /// <summary>A degenerate range that always rolls <paramref name="value"/>.</summary>
    public static FloatRange Single(float value) => new(value, value);

    /// <summary>Roll a uniform value in <c>[Min, Max]</c>; a collapsed range returns Min.</summary>
    public float Roll(RandomNumberGenerator rng) => Max <= Min ? Min : rng.RandfRange(Min, Max);
}
