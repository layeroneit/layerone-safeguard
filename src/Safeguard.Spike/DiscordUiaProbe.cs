using System.Diagnostics;
using System.Text;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.UIA3;

namespace LayerOne.Safeguard.Spike;

internal static class DiscordUiaProbe
{
    public static ProbeResult Run()
    {
        var process = ProcessFinder.FindFirst(ProcessFinder.DiscordNames);
        if (process is null)
        {
            return ProbeResult.Skipped("Discord is not running with a visible window.");
        }

        try
        {
            using var automation = new UIA3Automation();
            var app = Application.Attach(process);
            var window = app.GetMainWindow(automation, TimeSpan.FromSeconds(8));
            if (window is null)
            {
                return ProbeResult.Fail($"Attached to PID {process.Id} but no main window was found.");
            }

            var lines = CollectText(window);
            if (lines.Count == 0)
            {
                return ProbeResult.Fail(
                    "UIA attached, but no text nodes were found. Discord may need Accessibility enabled.");
            }

            var preview = string.Join('\n', lines.Take(12));
            return ProbeResult.Ok(
                $"Read {lines.Count} text node(s) from Discord (PID {process.Id}).",
                preview);
        }
        catch (Exception ex)
        {
            return ProbeResult.Fail($"Discord UIA failed: {ex.Message}");
        }
    }

    private static List<string> CollectText(AutomationElement root)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var lines = new List<string>();

        AutomationElement[] nodes;
        try
        {
            nodes = root.FindAllDescendants();
        }
        catch (Exception)
        {
            return lines;
        }

        foreach (var node in nodes)
        {
            ControlType type;
            try
            {
                type = node.ControlType;
            }
            catch
            {
                continue;
            }

            if (type is not (ControlType.Text or ControlType.ListItem or ControlType.Document
                or ControlType.Edit or ControlType.Hyperlink))
            {
                continue;
            }

            var text = ReadNode(node);
            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            text = Collapse(text);
            if (text.Length < 2 || !seen.Add(text))
            {
                continue;
            }

            lines.Add(text);
            if (lines.Count >= 80)
            {
                break;
            }
        }

        return lines;
    }

    private static string ReadNode(AutomationElement node)
    {
        try
        {
            if (node.Patterns.Value.IsSupported)
            {
                var value = node.Patterns.Value.Pattern.Value;
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }
        }
        catch
        {
            // Fall through to Name.
        }

        try
        {
            return node.Name ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string Collapse(string text)
    {
        var builder = new StringBuilder(text.Length);
        var space = false;
        foreach (var ch in text.Trim())
        {
            if (char.IsWhiteSpace(ch))
            {
                if (!space)
                {
                    builder.Append(' ');
                    space = true;
                }
            }
            else
            {
                builder.Append(ch);
                space = false;
            }
        }

        var result = builder.ToString();
        return result.Length > 240 ? result[..240] + "…" : result;
    }
}
