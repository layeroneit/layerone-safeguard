namespace LayerOne.Safeguard.Alerts;

/// <summary>
/// Ready-to-send parent email: plain text plus HTML with the Safeguard mark in the signature.
/// </summary>
public sealed class AlertCopy
{
    /// <summary>From / display name: LayerOne App Safeguard.</summary>
    public string FromDisplayName { get; init; } = "";

    public string Subject { get; init; } = "";

    /// <summary>Plain-text body, for mail apps that do not show HTML.</summary>
    public string Body { get; init; } = "";

    /// <summary>HTML body. The logo is referenced as cid:<see cref="EmailBuilder.LogoContentId"/>.</summary>
    public string HtmlBody { get; init; } = "";
}
