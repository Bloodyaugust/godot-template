using System.Collections.Generic;
using Godot;
using godottemplate.Core.Content;
using godottemplate.Server;

namespace godottemplate.Content;

/// <summary>
/// Autoload that loads and validates every definitions file at boot (<see cref="_EnterTree"/>),
/// so a bad file fails loudly on start-up with one <c>[Content] file (entry): reason</c> line
/// instead of surfacing mid-scene. The first failure latches into <see cref="Error"/> (already
/// logged by <see cref="ContentException"/>) and the game stays bootable, so an agent can read
/// the failure over <c>/content/state</c> (group <c>agent_content</c>, <see cref="IAgentContent"/>).
/// </summary>
/// <remarks>Add one line per definitions file to <see cref="LoadAll"/>; a game with several
/// registries calls each one's load (and a cross-file <c>ValidateReferences()</c> pass) there.</remarks>
public partial class ContentBootstrap : Node, IAgentContent
{
    public const string AgentGroup = "agent_content";

    private static ContentBootstrap _instance;

    /// <summary>The first content failure at boot, or null when everything loaded.</summary>
    public static string Error => _instance?._error;

    /// <summary>The placeholder definitions (empty when the load failed). Delete with <see cref="ExampleDef"/>.</summary>
    public static IReadOnlyList<ExampleDef> Examples => _instance?._examples ?? System.Array.Empty<ExampleDef>();

    private readonly List<string> _files = new();
    private IReadOnlyList<ExampleDef> _examples = System.Array.Empty<ExampleDef>();
    private string _error;
    private bool _loaded;

    public override void _EnterTree()
    {
        _instance = this;
        AddToGroup(AgentGroup);
        try
        {
            LoadAll();
            _loaded = true;
            GD.Print($"[Content] loaded {_files.Count} definitions file(s): {_examples.Count} examples");
        }
        catch (ContentException e)
        {
            _error = e.Message;   // already logged with GD.PushError by ContentException
        }
    }

    public override void _ExitTree()
    {
        if (_instance == this) _instance = null;
    }

    private void LoadAll()
    {
        _examples = Load(ExampleContent.FileName, ExampleContent.Parse);
    }

    /// <summary>Read one definitions file and parse it engine-free, bridging a <see cref="ContentError"/>.</summary>
    private T Load<T>(string fileName, System.Func<string, T> parse)
    {
        string text = ContentLoader.ReadText(fileName);
        try
        {
            var result = parse(text);
            _files.Add(fileName);
            return result;
        }
        catch (ContentError e)
        {
            throw ContentException.From(e);
        }
    }

    public Dictionary<string, object> GetContentState()
    {
        var examples = new List<object>();
        foreach (var d in _examples)
        {
            examples.Add(new Dictionary<string, object>
            {
                ["id"] = d.Id,
                ["name"] = d.Name,
                ["cost"] = d.Cost,
                ["kind"] = JsonDefReader.ToSnake(d.Kind.ToString()),
            });
        }
        return new Dictionary<string, object>
        {
            ["loaded"] = _loaded,
            ["error"] = _error,
            ["files"] = new List<string>(_files),
            ["example_count"] = _examples.Count,
            ["examples"] = examples,
        };
    }
}
