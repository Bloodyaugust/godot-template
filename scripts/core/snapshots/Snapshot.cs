using System;
using System.Collections;
using System.Collections.Generic;
using godottemplate.Core.Content;

namespace godottemplate.Core.Snapshots;

/// <summary>
/// Builders for agent-state snapshots — the plain, JSON-ready dictionaries every
/// <c>GetXState()</c> returns (<c>CLAUDE.md</c>, <i>Typed domain interfaces</i>). The convention
/// they enforce: a snapshot is a <see cref="Dictionary{TKey, TValue}"/> of <c>string</c> →
/// <c>object</c> with <b>snake_case keys</b>, whose values are only <c>null</c>, <c>string</c>,
/// <c>bool</c>, a number that survives a JSON double (<c>int</c> and narrower, <c>uint</c>,
/// <c>float</c>, <c>double</c>), or a nested snapshot / list built the same way. <b>64-bit
/// integers are strings</b> (<see cref="Wide(long)"/>): a seed or an id past 2^53 doesn't survive
/// a JSON number. Enums are their snake_case names (<see cref="Name"/>).
/// <para>A thing's snapshot is a static <c>XSnapshot.Of(x)</c> (or a <c>GetState()</c>) next to
/// the thing itself, built with <see cref="Of"/> — so a bad key or a raw <c>long</c> throws at
/// the first call under the unit tests or a Hurl run, not in a client's parser.</para>
/// </summary>
public static class Snapshot
{
    /// <summary>
    /// A snapshot of the given fields, in order. Throws <see cref="ArgumentException"/> on a key
    /// that isn't snake_case, a duplicate key, or a value outside the convention.
    /// </summary>
    public static Dictionary<string, object> Of(params (string Key, object Value)[] fields)
    {
        var snapshot = new Dictionary<string, object>(fields.Length);
        foreach (var (key, value) in fields)
        {
            if (!IsSnakeCase(key)) throw new ArgumentException($"snapshot key '{key}' is not snake_case");
            Check(value, key);
            if (!snapshot.TryAdd(key, value)) throw new ArgumentException($"snapshot key '{key}' appears twice");
        }
        return snapshot;
    }

    /// <summary>A list of <paramref name="items"/>, each mapped through <paramref name="each"/> and checked like a field value.</summary>
    public static List<object> List<T>(IEnumerable<T> items, Func<T, object> each)
    {
        var list = new List<object>();
        if (items == null) return list;
        foreach (var item in items)
        {
            var value = each(item);
            Check(value, $"[{list.Count}]");
            list.Add(value);
        }
        return list;
    }

    /// <summary>A 64-bit integer as its decimal text (exact, unlike a JSON number past 2^53).</summary>
    public static string Wide(long value) => value.ToString(System.Globalization.CultureInfo.InvariantCulture);

    /// <inheritdoc cref="Wide(long)"/>
    public static string Wide(ulong value) => value.ToString(System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>An optional 64-bit integer as its decimal text, or null.</summary>
    public static string Wide(ulong? value) => value.HasValue ? Wide(value.Value) : null;

    /// <summary>An enum value as its snake_case name (<c>FadePending</c> → <c>fade_pending</c>).</summary>
    public static string Name(Enum value) => value == null ? null : JsonDefReader.ToSnake(value.ToString());

    /// <summary>Whether <paramref name="key"/> is snake_case: lower-case letters and digits in words joined by single underscores, starting with a letter.</summary>
    public static bool IsSnakeCase(string key)
    {
        if (string.IsNullOrEmpty(key) || key[0] < 'a' || key[0] > 'z' || key[^1] == '_') return false;
        for (int i = 1; i < key.Length; i++)
        {
            char c = key[i];
            if (c == '_') { if (key[i - 1] == '_') return false; }
            else if (!(c >= 'a' && c <= 'z') && !(c >= '0' && c <= '9')) return false;
        }
        return true;
    }

    private static void Check(object value, string where)
    {
        switch (value)
        {
            case null:
            case string:
            case bool:
            case int or short or byte or sbyte or ushort or uint:
            case float or double:
                return;
            case long or ulong:
                throw new ArgumentException($"snapshot value at '{where}' is a 64-bit integer; use Snapshot.Wide (a string) — a JSON number loses precision past 2^53");
            case Enum:
                throw new ArgumentException($"snapshot value at '{where}' is an enum; use Snapshot.Name (its snake_case name)");
            case IDictionary<string, object>:
            case IList:
                return; // built by Of / List, which checked their own contents
            default:
                throw new ArgumentException($"snapshot value at '{where}' is a {value.GetType().Name}; only null, strings, bools, JSON-safe numbers, snapshots and lists belong in a snapshot");
        }
    }
}
