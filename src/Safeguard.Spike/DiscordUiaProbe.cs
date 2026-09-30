using System.Text;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.UIA3;

namespace LayerOne.Safeguard.Spike;

internal static class DiscordUiaProbe
{
    public static ProbeResult Run()
    {
        var windows = ProcessFinder.FindDiscord();
        if (windows.Count == 0)
        {
            return ProbeResult.Skipped("Discord is not running with a top-level window.");
        }

        var errors = new List<string>();
        foreach (var target in windows)
        {
            try
            {
                var result = ProbeWindow(target);
                if (result.Passed)
                {
                    return result;
                }

                errors.Add($"{Describe(target)}: {result.Summary}");
            }
            catch (Exception ex)
            {
                errors.Add($"{Describe(target)}: {ex.GetType().Name}: {ex.Message}");
            }
        }

        return ProbeResult.Fail(
            "Discord windows were found, but no chat text was readable from the accessibility tree.",
            string.Join('\n', errors) + AccessibilityHint);
    }

    private static ProbeResult ProbeWindow(WindowTarget target)
    {
        using var automation = new UIA3Automation();
        var root = automation.FromHandle(target.Hwnd);
        if (root is null)
        {
            return ProbeResult.Fail("FromHandle returned no element.");
        }

        var lines = CollectRawText(root, automation);
        var chatRoot = FindChatRoot(root);
        if (chatRoot is not null)
        {
            var chatLines = CollectRawText(chatRoot, automation);
            if (chatLines.Count > 0)
            {
                lines = chatLines;
            }
        }

        if (lines.Count == 0)
        {
            return ProbeResult.Fail(
                "UIA attached, but the raw tree had no named nodes. Chromium accessibility is likely off.");
        }

        var preview = string.Join('\n', lines.Take(12));
        if (chatRoot is null && !LooksLikeChat(lines))
        {
            return ProbeResult.Fail(
                $"Read {lines.Count} chrome label(s), but no Messages list and no chat-like lines. Open a channel.",
                preview);
        }

        return ProbeResult.Ok(
            $"Read {lines.Count} text node(s) from Discord (PID {target.ProcessId}, \"{target.Title}\").",
            preview);
    }

    private static AutomationElement? FindChatRoot(AutomationElement window)
    {
        try
        {
            return window.FindFirstDescendant(cf =>
                cf.ByName("Messages").And(cf.ByControlType(ControlType.List)))
                ?? window.FindFirstDescendant(cf => cf.ByName("Messages"))
                ?? window.FindFirstDescendant(cf =>
                    cf.ByControlType(ControlType.List).And(cf.ByName("Message list")));
        }
        catch
        {
            return null;
        }
    }

    private static List<string> CollectRawText(AutomationElement root, UIA3Automation automation)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var lines = new List<string>();
        var walker = automation.TreeWalkerFactory.GetRawViewWalker();
        Walk(walker, root, seen, lines, 0);
        return lines;
    }

    private static void Walk(
        FlaUI.Core.ITreeWalker walker,
        AutomationElement node,
        HashSet<string> seen,
        List<string> lines,
        int depth)
    {
        if (depth > 30 || lines.Count >= 80)
        {
            return;
        }

        var text = ReadNode(node);
        if (!string.IsNullOrWhiteSpace(text))
        {
            text = Collapse(text);
            if (text.Length >= 2 && seen.Add(text))
            {
                lines.Add(text);
            }
        }

        AutomationElement? child = null;
        try
        {
            child = walker.GetFirstChild(node);
        }
        catch
        {
            return;
        }

        while (child is not null && lines.Count < 80)
        {
            Walk(walker, child, seen, lines, depth + 1);
            try
            {
                child = walker.GetNextSibling(child);
            }
            catch
            {
                break;
            }
        }
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
            if (node.Patterns.LegacyIAccessible.IsSupported)
            {
                var legacy = node.Patterns.LegacyIAccessible.Pattern.Name;
                if (!string.IsNullOrWhiteSpace(legacy))
                {
                    return legacy;
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

    private static bool LooksLikeChat(IReadOnlyList<string> lines)
    {
        return lines.Any(line =>
            line.Contains("Today at", StringComparison.OrdinalIgnoreCase)
            || line.Contains("Yesterday at", StringComparison.OrdinalIgnoreCase)
            || (line.Length >= 24 && line.Contains(' ')));
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

    private static string Describe(WindowTarget target) =>
        $"PID {target.ProcessId} \"{target.Title}\" [{target.ClassName}]";

    private const string AccessibilityHint =
        "\nIf the tree is empty, fully quit Discord (tray too) and start it with " +
        "--force-renderer-accessibility, then open a text channel.";
}
