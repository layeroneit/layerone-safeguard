using LayerOne.Safeguard.Brand;

namespace LayerOne.Safeguard.Alerts;

/// <summary>
/// Builds parent-readable alert email copy signed by the app name.
/// </summary>
public static class AlertCopyWriter
{
    public const string FromDisplayName = Colors.ProductName;

    public static AlertCopy Build(AlertMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        var app = AppLabel(message.AppName);
        var about = OptionalLine(message.Category);
        var when = message.Time.ToLocalTime().ToString("dddd, MMMM d, yyyy 'at' h:mm tt");

        var subject = about is null
            ? $"{Colors.ShortName} notice: something in {app} to review"
            : $"{Colors.ShortName} notice: something in {app} about {about}";
        if (message.Urgent)
        {
            subject = subject.Replace($"{Colors.ShortName} notice:", $"{Colors.ShortName} notice (please look soon):");
        }

        if (message.IsTest)
        {
            subject = subject.Replace($"{Colors.ShortName} notice", $"{Colors.ShortName} notice (test)");
        }

        var email = new EmailBuilder().Paragraph("Hello,");
        if (message.IsTest)
        {
            email.Callout("This is a test alert",
                "You started this from the Safeguard window. No one said this in Discord or Roblox. This is what a real alert looks like.",
                Colors.SecondaryText);
        }

        email
            .Paragraph($"Safeguard spotted something in {app} that you may want to look at.")
            .Facts(("When", when), ("App", app), ("About", about))
            .FlaggedWords(message.FlaggedWords)
            .Paragraph("Here is a short piece of what was said:")
            .Quote(ShortSnippet(message.Snippet));

        var tip = OptionalLine(message.TalkItOver);
        if (tip is not null)
        {
            email.Callout("A calm way to bring it up:", tip, Colors.Primary);
        }

        if (message.Urgent)
        {
            email.Callout("Please look soon",
                "Safeguard saw more than one warning sign together. If you think your child is in danger right now, call 911.",
                Colors.Risk);
        }

        email.Paragraph("Safeguard only sends a notice. It does not block or delete anything. You decide what to do next.");
        return email.Build(subject);
    }

    private static string AppLabel(string? appName)
    {
        if (string.IsNullOrWhiteSpace(appName))
        {
            return "Discord or Roblox";
        }

        var trimmed = appName.Trim();
        if (trimmed.Equals("Discord", StringComparison.OrdinalIgnoreCase))
        {
            return "Discord";
        }

        if (trimmed.Equals("Roblox", StringComparison.OrdinalIgnoreCase))
        {
            return "Roblox";
        }

        return trimmed;
    }

    private static string? OptionalLine(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string ShortSnippet(string? snippet)
    {
        if (string.IsNullOrWhiteSpace(snippet))
        {
            return "Safeguard could not include a snippet.";
        }

        var text = snippet.Trim();
        const int max = 280;
        return text.Length <= max ? text : text[..(max - 1)].TrimEnd() + "…";
    }
}
