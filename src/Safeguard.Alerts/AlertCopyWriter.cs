using System.Text;
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
        var snippet = ShortSnippet(message.Snippet);
        var when = message.Time.ToLocalTime().ToString("dddd, MMMM d, yyyy 'at' h:mm tt");

        var subject = about is null
            ? $"{Colors.ShortName} notice: something in {app} to review"
            : $"{Colors.ShortName} notice: something in {app} about {about}";
        if (message.Urgent)
        {
            subject = subject.Replace($"{Colors.ShortName} notice:", $"{Colors.ShortName} notice (please look soon):");
        }

        var body = new StringBuilder();
        body.AppendLine("Hello,");
        body.AppendLine();
        body.Append("Safeguard spotted something in ");
        body.Append(app);
        body.AppendLine(" that you may want to look at.");
        body.AppendLine();
        body.Append("When: ");
        body.AppendLine(when);
        body.Append("App: ");
        body.AppendLine(app);
        if (about is not null)
        {
            body.Append("About: ");
            body.AppendLine(about);
        }

        body.AppendLine();
        body.AppendLine("Here is a short piece of what was said:");
        body.AppendLine();
        body.Append('"');
        body.Append(snippet);
        body.AppendLine("\"");
        body.AppendLine();
        var tip = OptionalLine(message.TalkItOver);
        if (tip is not null)
        {
            body.AppendLine("A calm way to bring it up:");
            body.AppendLine(tip);
            body.AppendLine();
        }

        if (message.Urgent)
        {
            body.AppendLine("Safeguard saw more than one warning sign together. If you think your child is in danger right now, call 911.");
            body.AppendLine();
        }

        body.AppendLine("Safeguard only sends a notice. It does not block or delete anything. You decide what to do next.");
        body.AppendLine();
        body.AppendLine(Colors.ShortName);
        body.Append(Colors.Publisher);

        return new AlertCopy
        {
            FromDisplayName = FromDisplayName,
            Subject = subject,
            Body = body.ToString()
        };
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

    private static string? OptionalLine(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }

    private static string ShortSnippet(string? snippet)
    {
        if (string.IsNullOrWhiteSpace(snippet))
        {
            return "Safeguard could not include a snippet.";
        }

        var text = snippet.Trim();
        const int max = 280;
        if (text.Length <= max)
        {
            return text;
        }

        return text[..(max - 1)].TrimEnd() + "…";
    }
}
