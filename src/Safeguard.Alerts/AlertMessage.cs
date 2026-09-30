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
}
