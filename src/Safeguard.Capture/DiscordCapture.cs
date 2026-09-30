using System.Text;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.UIA3;

namespace LayerOne.Safeguard.Capture;

public static class DiscordCapture
{
    public static CaptureRead TryRead()
    {
        var windows = NativeWindows.FindDiscord();
        if (windows.Count == 0)
        {
            return CaptureRead.Idle("Discord is not open.");
        }

        var preferred = windows
            .Where(w => !w.Title.Contains("Friends - Discord", StringComparison.OrdinalIgnoreCase))
            .ToList();
        var order = preferred.Count > 0 ? preferred.Concat(windows.Except(preferred)) : windows;

        foreach (var target in order)
        {
            try
            {
                var read = ProbeWindow(target);
                if (read.Ok && !string.IsNullOrWhiteSpace(read.Text))
                {
                    return read;
                }
            }
            catch (Exception ex)
            {
                return CaptureRead.Fail($"Could not read Discord: {ex.Message}");
            }
        }

        return CaptureRead.Fail("Discord is open, but no chat text was readable yet.");
    }

    private static CaptureRead ProbeWindow(WindowTarget target)
    {
        using var automation = new UIA3Automation();
        var root = automation.FromHandle(target.Hwnd);
        if (root is null)
        {
            return CaptureRead.Fail("Could not attach to the Discord window.");
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
            return CaptureRead.Fail("Discord did not expose any text. Accessibility may be off.");
        }

        var preview = string.Join('\n', lines.Take(16));
        if (target.Title.Contains("Friends - Discord", StringComparison.OrdinalIgnoreCase))
        {
            return CaptureRead.OkText(
                "Discord is on the Friends page. Open a chat to watch messages.",
                preview);
        }

        return CaptureRead.OkText(
            $"Reading Discord — {target.Title}",
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
            // Fall through.
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
            // Fall through.
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
