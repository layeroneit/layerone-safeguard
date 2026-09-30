using System.Text.Json;

namespace LayerOne.Safeguard.Scoring;

/// <summary>
/// Versioned phrase taxonomy. Source of truth is phrases/taxonomy.v1.json, embedded at build.
/// </summary>
public sealed class Taxonomy
{
    public int Version { get; init; }
    public Thresholds Thresholds { get; init; } = new();
    public List<Combo> Combos { get; init; } = new();
    public List<Category> Categories { get; init; } = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    public static Taxonomy LoadEmbedded()
    {
        using var stream = typeof(Taxonomy).Assembly.GetManifestResourceStream("taxonomy.v1.json")
            ?? throw new InvalidOperationException("taxonomy.v1.json is not embedded.");
        return Load(stream);
    }

    public static Taxonomy Load(Stream json) =>
        JsonSerializer.Deserialize<Taxonomy>(json, JsonOptions)
        ?? throw new InvalidOperationException("Taxonomy file is empty.");
}

public sealed class Thresholds
{
    public int Review { get; init; } = 3;
    public int Risk { get; init; } = 7;
}

public sealed class Combo
{
    public List<string> Categories { get; init; } = new();
    public int Bonus { get; init; }
}

public sealed class Category
{
    public string Id { get; init; } = "";

    /// <summary>Plain-language name for parents, e.g. "meeting in person".</summary>
    public string Label { get; init; } = "";

    public int Weight { get; init; } = 1;

    /// <summary>One calm line for the parent on how to bring this up with their child.</summary>
    public string TalkItOver { get; init; } = "";

    public List<string> Phrases { get; init; } = new();
    public List<string> Weak { get; init; } = new();
}
