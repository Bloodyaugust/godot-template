using System;
using Godot;
using godottemplate.Util;

namespace godottemplate.Content;

/// <summary>
/// A typed, fail-loud accessor over a single parsed JSON object (one definition entry,
/// or a nested block within one). Wraps a Godot <see cref="Godot.Collections.Dictionary"/>
/// and converts fields to C# types, throwing a clear, contextual
/// <see cref="ContentException"/> on a missing required key, a wrong type, or an unknown
/// enum value — so malformed content fails obviously at load rather than producing a
/// silent empty world.
/// </summary>
public sealed class DefReader
{
    private readonly Godot.Collections.Dictionary _dict;

    /// <summary>The source file name (for error messages).</summary>
    public string File { get; }

    /// <summary>What this object is, for error messages (e.g. <c>structure 'mining_rig'</c>).</summary>
    public string Context { get; }

    public DefReader(Godot.Collections.Dictionary dict, string file, string context)
    {
        _dict = dict ?? throw ContentException.For(file, context, "expected a JSON object, got null");
        File = file;
        Context = context;
    }

    public bool Has(string key) => _dict.ContainsKey(key);

    private Variant Require(string key)
    {
        if (!_dict.ContainsKey(key))
            throw ContentException.For(File, Context, $"missing required field '{key}'");
        return _dict[key];
    }

    public string ReqString(string key) => Require(key).AsString();
    public string OptString(string key, string fallback = null)
        => _dict.ContainsKey(key) ? _dict[key].AsString() : fallback;

    public int ReqInt(string key) => Require(key).AsInt32();
    public int OptInt(string key, int fallback = 0)
        => _dict.ContainsKey(key) ? _dict[key].AsInt32() : fallback;

    public float ReqFloat(string key) => (float)Require(key).AsDouble();
    public float OptFloat(string key, float fallback = 0f)
        => _dict.ContainsKey(key) ? (float)_dict[key].AsDouble() : fallback;

    public bool OptBool(string key, bool fallback = false)
        => _dict.ContainsKey(key) ? _dict[key].AsBool() : fallback;

    /// <summary>An optional string array; empty (never null) when the key is absent.</summary>
    public string[] OptStringArray(string key)
    {
        if (!_dict.ContainsKey(key)) return Array.Empty<string>();
        var arr = _dict[key].AsGodotArray();
        var result = new string[arr.Count];
        for (int i = 0; i < arr.Count; i++) result[i] = arr[i].AsString();
        return result;
    }

    /// <summary>
    /// A required <c>[min, max]</c> integer range (a single number collapses to <c>[n, n]</c>).
    /// Range-valued fields are rolled per realization by the consuming generator.
    /// </summary>
    public IntRange ReqIntRange(string key) => ParseIntRange(Require(key), key);
    public IntRange OptIntRange(string key, IntRange fallback)
        => _dict.ContainsKey(key) ? ParseIntRange(_dict[key], key) : fallback;

    /// <summary>A required <c>[min, max]</c> float range (a single number collapses to <c>[n, n]</c>).</summary>
    public FloatRange ReqFloatRange(string key) => ParseFloatRange(Require(key), key);
    public FloatRange OptFloatRange(string key, FloatRange fallback)
        => _dict.ContainsKey(key) ? ParseFloatRange(_dict[key], key) : fallback;

    private IntRange ParseIntRange(Variant v, string key)
    {
        if (v.VariantType == Variant.Type.Array)
        {
            var arr = v.AsGodotArray();
            if (arr.Count == 1) return IntRange.Single(arr[0].AsInt32());
            if (arr.Count == 2) return new IntRange(arr[0].AsInt32(), arr[1].AsInt32());
            throw ContentException.For(File, Context, $"field '{key}' must be a [min, max] range (1 or 2 numbers)");
        }
        return IntRange.Single(v.AsInt32());
    }

    private FloatRange ParseFloatRange(Variant v, string key)
    {
        if (v.VariantType == Variant.Type.Array)
        {
            var arr = v.AsGodotArray();
            if (arr.Count == 1) return FloatRange.Single((float)arr[0].AsDouble());
            if (arr.Count == 2) return new FloatRange((float)arr[0].AsDouble(), (float)arr[1].AsDouble());
            throw ContentException.For(File, Context, $"field '{key}' must be a [min, max] range (1 or 2 numbers)");
        }
        return FloatRange.Single((float)v.AsDouble());
    }

    /// <summary>An optional <c>[x, y]</c> pair as a <see cref="Vector2"/>, or the fallback when absent.</summary>
    public Vector2 OptVector2(string key, Vector2 fallback)
    {
        if (!_dict.ContainsKey(key)) return fallback;
        var v = _dict[key];
        if (v.VariantType != Variant.Type.Array)
            throw ContentException.For(File, Context, $"field '{key}' must be an [x, y] array");
        var arr = v.AsGodotArray();
        if (arr.Count != 2)
            throw ContentException.For(File, Context, $"field '{key}' must be an [x, y] pair");
        return new Vector2((float)arr[0].AsDouble(), (float)arr[1].AsDouble());
    }

    /// <summary>A required nested object, returned as its own reader scoped to the same file.</summary>
    public DefReader ReqChild(string key, string childContext = null)
    {
        var v = Require(key);
        if (v.VariantType != Variant.Type.Dictionary)
            throw ContentException.For(File, Context, $"field '{key}' must be a JSON object");
        return new DefReader(v.AsGodotDictionary(), File, childContext ?? $"{Context} > {key}");
    }

    /// <summary>An optional nested object, or null when the key is absent.</summary>
    public DefReader OptChild(string key, string childContext = null)
        => _dict.ContainsKey(key) ? ReqChild(key, childContext) : null;

    /// <summary>A required array of nested objects, each wrapped in its own reader.</summary>
    public DefReader[] ReqChildArray(string key, string childContext = null)
    {
        var v = Require(key);
        if (v.VariantType != Variant.Type.Array)
            throw ContentException.For(File, Context, $"field '{key}' must be a JSON array");
        var arr = v.AsGodotArray();
        var result = new DefReader[arr.Count];
        for (int i = 0; i < arr.Count; i++)
        {
            if (arr[i].VariantType != Variant.Type.Dictionary)
                throw ContentException.For(File, Context, $"'{key}[{i}]' must be a JSON object");
            result[i] = new DefReader(arr[i].AsGodotDictionary(), File, $"{childContext ?? Context}[{i}]");
        }
        return result;
    }
}
