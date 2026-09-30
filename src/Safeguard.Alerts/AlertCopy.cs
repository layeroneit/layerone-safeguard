namespace LayerOne.Safeguard.Alerts;

/// <summary>
/// Ready-to-send parent email text. SMTP is a later step.
/// </summary>
public sealed class AlertCopy
{
    /// <summary>From / display name: LayerOne App Safeguard.</summary>
    public string FromDisplayName { get; init; } = "";

    public string Subject { get; init; } = "";

    public string Body { get; init; } = "";
}
