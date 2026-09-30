namespace LayerOne.Safeguard.Core;

/// <summary>
/// One read that scored "needs a look" or higher. This is the only chat text
/// Safeguard keeps; clear reads are never stored.
/// </summary>
public sealed record FlaggedItem
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTimeOffset Time { get; init; }
    public string App { get; init; } = "";

    /// <summary>"Review" or "Risk".</summary>
    public string Level { get; init; } = "Review";

    public int Score { get; init; }

    /// <summary>Plain-language category of the strongest hit, e.g. "meeting in person".</summary>
    public string Label { get; init; } = "";

    /// <summary>The line that triggered the flag.</summary>
    public string Snippet { get; init; } = "";

    public string TalkItOver { get; init; } = "";

    /// <summary>Every matched line with its category id, for the raw log.</summary>
    public List<FlaggedLine> Lines { get; init; } = new();

    /// <summary>The whole read the flag came from (one chat window read), for context.</summary>
    public string Context { get; init; } = "";

    /// <summary>The exact words that set off the flag, strongest first, no repeats.</summary>
    public IReadOnlyList<string> FlaggedWords() =>
        Lines.Select(l => string.IsNullOrWhiteSpace(l.Matched) ? l.Phrase : l.Matched)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    /// <summary>Started from the "Send a test alert" button. Not real chat.</summary>
    public bool IsTest { get; init; }

    public bool IsRisk => Level.Equals("Risk", StringComparison.OrdinalIgnoreCase);
}

public sealed record FlaggedLine(string Category, string Phrase, string Line, string Matched = "");
