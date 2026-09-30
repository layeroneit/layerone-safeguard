using System.Net;
using System.Text;
using LayerOne.Safeguard.Brand;

namespace LayerOne.Safeguard.Alerts;

/// <summary>
/// Builds one email as plain text and as simple HTML, from the same blocks, so
/// every mail app shows the same words. The HTML signature carries the Safeguard
/// mark as an inline image (content id <see cref="LogoContentId"/>).
/// </summary>
public sealed class EmailBuilder
{
    public const string LogoContentId = "safeguard-logo";

    private readonly StringBuilder _text = new();
    private readonly StringBuilder _html = new();

    public EmailBuilder Paragraph(string text)
    {
        _text.AppendLine(text).AppendLine();
        _html.Append($"<p style=\"margin:0 0 14px 0;\">{Encode(text)}</p>");
        return this;
    }

    public EmailBuilder Facts(params (string Label, string? Value)[] facts)
    {
        var rows = facts.Where(f => !string.IsNullOrWhiteSpace(f.Value)).ToList();
        if (rows.Count == 0)
        {
            return this;
        }

        _html.Append("<table role=\"presentation\" style=\"border-collapse:collapse;margin:0 0 14px 0;\">");
        foreach (var (label, value) in rows)
        {
            _text.AppendLine($"{label}: {value}");
            _html.Append($"<tr><td style=\"padding:2px 14px 2px 0;color:{Colors.SecondaryText};\">{Encode(label)}</td>" +
                         $"<td style=\"padding:2px 0;\">{Encode(value!)}</td></tr>");
        }

        _html.Append("</table>");
        _text.AppendLine();
        return this;
    }

    /// <summary>The words that set off the alert, as [tags].</summary>
    public EmailBuilder FlaggedWords(IReadOnlyList<string> words)
    {
        if (words.Count == 0)
        {
            return this;
        }

        _text.AppendLine("Flagged words: " + string.Join(" ", words.Select(w => $"[{w}]"))).AppendLine();
        _html.Append($"<p style=\"margin:0 0 14px 0;\"><span style=\"color:{Colors.SecondaryText};\">Flagged words:</span> ");
        foreach (var word in words)
        {
            _html.Append($"<span style=\"display:inline-block;margin:2px 4px 2px 0;padding:2px 8px;border-radius:6px;" +
                         $"background:#F3E8FF;color:{Colors.Primary};font-family:Consolas,monospace;font-weight:bold;\">[{Encode(word)}]</span>");
        }

        _html.Append("</p>");
        return this;
    }

    public EmailBuilder Quote(string text)
    {
        _text.AppendLine($"\"{text}\"").AppendLine();
        _html.Append($"<blockquote style=\"margin:0 0 14px 0;padding:10px 14px;background:{Colors.Canvas};" +
                     $"border-left:4px solid {Colors.Accent};font-style:italic;\">“{Encode(text)}”</blockquote>");
        return this;
    }

    /// <summary>A highlighted box, e.g. the talk-it-over tip or the urgent note.</summary>
    public EmailBuilder Callout(string heading, string text, string colour)
    {
        _text.AppendLine(heading).AppendLine(text).AppendLine();
        _html.Append($"<div style=\"margin:0 0 14px 0;padding:12px 14px;border-radius:8px;background:{Colors.Canvas};border-left:4px solid {colour};\">" +
                     $"<div style=\"font-weight:bold;color:{colour};margin-bottom:4px;\">{Encode(heading)}</div>{Encode(text)}</div>");
        return this;
    }

    public AlertCopy Build(string subject)
    {
        var text = new StringBuilder(_text.ToString())
            .AppendLine(Colors.ShortName)
            .Append(Colors.Publisher)
            .ToString();

        var html = new StringBuilder()
            .Append("<!DOCTYPE html><html><body style=\"margin:0;padding:0;background:#FFFFFF;\">")
            .Append($"<div style=\"max-width:600px;padding:20px;font-family:'Segoe UI',Arial,sans-serif;font-size:15px;line-height:1.5;color:{Colors.Ink};\">")
            .Append(_html)
            .Append("<table role=\"presentation\" style=\"border-collapse:collapse;margin-top:22px;border-top:1px solid #E6E1EE;\"><tr>")
            .Append($"<td style=\"padding:14px 12px 0 0;vertical-align:middle;\"><img src=\"cid:{LogoContentId}\" width=\"48\" height=\"48\" alt=\"Safeguard\" style=\"display:block;border:0;\"></td>")
            .Append($"<td style=\"padding:14px 0 0 0;vertical-align:middle;\"><div style=\"font-weight:bold;font-size:16px;color:{Colors.Ink};\">{Colors.ShortName}</div>")
            .Append($"<div style=\"font-size:13px;color:{Colors.SecondaryText};\">{Colors.Publisher}</div></td>")
            .Append("</tr></table></div></body></html>")
            .ToString();

        return new AlertCopy
        {
            FromDisplayName = Colors.ProductName,
            Subject = subject,
            Body = text,
            HtmlBody = html
        };
    }

    /// <summary>The Safeguard mark (PNG) for the signature.</summary>
    public static byte[] LogoPng()
    {
        using var stream = typeof(EmailBuilder).Assembly.GetManifestResourceStream("safeguard-logo.png")
            ?? throw new InvalidOperationException("Signature logo is not embedded.");
        using var copy = new MemoryStream();
        stream.CopyTo(copy);
        return copy.ToArray();
    }

    private static string Encode(string text) => WebUtility.HtmlEncode(text);
}
