namespace LayerOne.Safeguard.Alerts;

/// <summary>
/// Facts for one parent email. Keep this small; the writer turns it into copy.
/// </summary>
public sealed class AlertMessage
{
    /// <summary>Discord or Roblox (other names are shown as given).</summary>
    public string AppName { get; init; } = "";

    public DateTimeOffset Time { get; init; }

    /// <summary>A short piece of what was said. Do not pass a full chat log.</summary>
    public string Snippet { get; init; } = "";

    /// <summary>Optional plain-language reason, e.g. meeting up.</summary>
    public string? Category { get; init; }

    /// <summary>Optional calm suggestion for how to bring it up with the child.</summary>
    public string? TalkItOver { get; init; }

    /// <summary>The exact words that set off the alert, shown as [tags].</summary>
    public IReadOnlyList<string> FlaggedWords { get; init; } = Array.Empty<string>();

    /// <summary>Started by the parent from the app to check alerts. Not real chat.</summary>
    public bool IsTest { get; init; }

    /// <summary>Several warning signs at once. Adds a line about getting help now.</summary>
    public bool Urgent { get; init; }
}
