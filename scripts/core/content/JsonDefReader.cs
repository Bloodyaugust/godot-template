using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace godottemplate.Core.Content;

/// <summary>
/// A typed, fail-loud accessor over one JSON definition object, built on
/// <c>System.Text.Json</c> so definition parsing is engine-free and unit-testable. It parses
/// from injected text — the Godot side (<c>scripts/content/ContentLoader.cs</c>) reads the
/// <c>res://</c> file and hands the text over. Every failure throws a
/// <see cref="ContentError"/> naming the file and entry. Unknown keys are rejected when the
/// caller lists the known ones (<see cref="RejectUnknownKeys"/>), enums are snake_case, and
/// numbers are integers only: a fractional value is rejected, keeping content off floating
/// point (relax deliberately, with a <c>ReqFloat</c>, if a game wants floats).
/// </summary>
public sealed class JsonDefReader
{
    // Strict JSON (no comments, no trailing commas): the Godot editor also parses *.json
    // under res:// and would reject anything laxer, so the files must stay plain JSON.
    private static readonly JsonDocumentOptions DocOptions = new();

    private readonly JsonElement _obj;

    public string File { get; }
    public string Context { get; }

    public JsonDefReader(JsonElement obj, string file, string context)
    {
        File = file;
        Context = context;
        if (obj.ValueKind != JsonValueKind.Object)
            throw Fail($"expected a JSON object, got {obj.ValueKind}");
        _obj = obj;
    }

    /// <summary>
    /// Parse <paramref name="text"/> as a top-level JSON array and map each element through
    /// <paramref name="each"/> while the document is alive. Throws on malformed JSON or a
    /// non-array root.
    /// </summary>
    public static List<T> ParseArray<T>(string text, string file, Func<JsonElement, int, T> each)
    {
        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(text, DocOptions);
        }
        catch (JsonException e)
        {
            throw new ContentError(file, null, $"malformed JSON at line {(e.LineNumber ?? 0) + 1}: {e.Message}");
        }
        using (doc)
        {
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
                throw new ContentError(file, null, "expected a top-level JSON array of definitions");
            var result = new List<T>();
            int i = 0;
            foreach (var el in doc.RootElement.EnumerateArray())
                result.Add(each(el, i++));
            return result;
        }
    }

    public ContentError Fail(string reason) => new(File, Context, reason);

    /// <summary>Reject any key outside <paramref name="known"/> — catches typos like <c>max_helath</c>.</summary>
    public void RejectUnknownKeys(params string[] known)
    {
        var set = new HashSet<string>(known, StringComparer.Ordinal);
        foreach (var prop in _obj.EnumerateObject())
        {
            if (!set.Contains(prop.Name))
                throw Fail($"unknown key '{prop.Name}' (expected one of: {string.Join(", ", known)})");
        }
    }

    public bool Has(string key) => _obj.TryGetProperty(key, out var v) && v.ValueKind != JsonValueKind.Null;

    public string ReqString(string key)
    {
        var v = Req(key);
        if (v.ValueKind != JsonValueKind.String || string.IsNullOrEmpty(v.GetString()))
            throw Fail($"'{key}' must be a non-empty string");
        return v.GetString();
    }

    public string OptString(string key, string fallback = null)
        => Has(key) ? ReqString(key) : fallback;

    public int ReqInt(string key) => AsInt(Req(key), key);

    public int OptInt(string key, int fallback) => Has(key) ? ReqInt(key) : fallback;

    public bool OptBool(string key, bool fallback)
    {
        if (!Has(key)) return fallback;
        var v = Req(key);
        if (v.ValueKind != JsonValueKind.True && v.ValueKind != JsonValueKind.False)
            throw Fail($"'{key}' must be a boolean");
        return v.GetBoolean();
    }

    /// <summary>A string array; absent → empty. Entries must be non-empty strings.</summary>
    public List<string> OptStringList(string key)
    {
        var list = new List<string>();
        if (!Has(key)) return list;
        foreach (var el in ReqArray(key))
        {
            if (el.ValueKind != JsonValueKind.String || string.IsNullOrEmpty(el.GetString()))
                throw Fail($"'{key}' must contain only non-empty strings");
            list.Add(el.GetString());
        }
        return list;
    }

    public List<JsonElement> ReqArray(string key)
    {
        var v = Req(key);
        if (v.ValueKind != JsonValueKind.Array)
            throw Fail($"'{key}' must be an array");
        return new List<JsonElement>(v.EnumerateArray());
    }

    public List<JsonElement> OptArray(string key) => Has(key) ? ReqArray(key) : new List<JsonElement>();

    /// <summary>An <c>[x, y]</c> integer pair.</summary>
    public (int x, int y) ReqIntPair(string key)
    {
        var arr = ReqArray(key);
        if (arr.Count != 2)
            throw Fail($"'{key}' must be a two-element [x, y] integer array");
        return (AsInt(arr[0], key), AsInt(arr[1], key));
    }

    /// <summary>A <c>{"key": "value"}</c> object of strings, in file order; absent → empty.</summary>
    public List<KeyValuePair<string, string>> OptStringMap(string key)
    {
        var list = new List<KeyValuePair<string, string>>();
        if (!Has(key)) return list;
        foreach (var prop in ReqObject(key))
        {
            if (prop.Value.ValueKind != JsonValueKind.String || string.IsNullOrEmpty(prop.Value.GetString()))
                throw Fail($"'{key}.{prop.Name}' must be a non-empty string");
            list.Add(new KeyValuePair<string, string>(prop.Name, prop.Value.GetString()));
        }
        return list;
    }

    /// <summary>A <c>{"key": int}</c> object, in file order; absent → empty.</summary>
    public List<KeyValuePair<string, int>> OptIntMap(string key)
    {
        var list = new List<KeyValuePair<string, int>>();
        if (!Has(key)) return list;
        foreach (var prop in ReqObject(key))
            list.Add(new KeyValuePair<string, int>(prop.Name, AsInt(prop.Value, $"{key}.{prop.Name}")));
        return list;
    }

    /// <summary>
    /// A closed-set enum written in snake_case (<c>"clip_size"</c> → <c>ClipSize</c>).
    /// Unknown spellings fail loudly, listing the valid ones.
    /// </summary>
    public T ReqEnum<T>(string key) where T : struct, Enum => ParseEnum<T>(ReqString(key), key);

    public T ParseEnum<T>(string value, string what) where T : struct, Enum
    {
        foreach (T candidate in Enum.GetValues<T>())
        {
            if (ToSnake(candidate.ToString()) == value) return candidate;
        }
        var valid = new List<string>();
        foreach (T candidate in Enum.GetValues<T>()) valid.Add(ToSnake(candidate.ToString()));
        throw Fail($"unknown {what} '{value}' (expected one of: {string.Join(", ", valid)})");
    }

    /// <summary><c>ShieldRechargeDelay</c> → <c>shield_recharge_delay</c>.</summary>
    public static string ToSnake(string pascal)
    {
        var sb = new StringBuilder(pascal.Length + 4);
        for (int i = 0; i < pascal.Length; i++)
        {
            char c = pascal[i];
            if (char.IsUpper(c))
            {
                if (i > 0) sb.Append('_');
                sb.Append(char.ToLowerInvariant(c));
            }
            else
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }

    private JsonElement.ObjectEnumerator ReqObject(string key)
    {
        var v = Req(key);
        if (v.ValueKind != JsonValueKind.Object)
            throw Fail($"'{key}' must be an object");
        return v.EnumerateObject();
    }

    private JsonElement Req(string key)
    {
        if (!_obj.TryGetProperty(key, out var v) || v.ValueKind == JsonValueKind.Null)
            throw Fail($"missing required key '{key}'");
        return v;
    }

    private int AsInt(JsonElement v, string key)
    {
        if (v.ValueKind != JsonValueKind.Number || !v.TryGetInt32(out var i))
            throw Fail($"'{key}' must be an integer");
        return i;
    }
}
