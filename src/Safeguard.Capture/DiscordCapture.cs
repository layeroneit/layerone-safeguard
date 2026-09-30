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

        // A chat is open: read the messages themselves, newest last.
        var chatRoot = FindChatRoot(root);
        if (chatRoot is not null)
        {
            var messages = ReadMessages(chatRoot);
            if (messages.Count > 0)
            {
                return CaptureRead.OkText(
                    $"Reading Discord — {target.Title}",
                    string.Join('\n', messages.TakeLast(MaxMessages)));
            }
        }

        var lines = CollectRawText(root, automation);
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
            "Discord is open. Open a chat or channel to watch messages.",
            preview);
    }

    // Messages on screen passed to scoring each read. Discord keeps roughly this many loaded.
    private const int MaxMessages = 50;

    // Discord names the list "Messages in <channel or person>".
    private static AutomationElement? FindChatRoot(AutomationElement window)
    {
        try
        {
            return window
                .FindAllDescendants(cf => cf.ByControlType(ControlType.List))
                .FirstOrDefault(list =>
                {
                    var name = SafeName(list);
                    return name.StartsWith("Messages", StringComparison.OrdinalIgnoreCase)
                           || name.Equals("Message list", StringComparison.OrdinalIgnoreCase);
                });
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// One line per message: "Author: text". Discord gives each message a group
    /// named "Author , text , time"; when that is missing, the message's text nodes are used.
    /// </summary>
    private static List<string> ReadMessages(AutomationElement list)
    {
        var messages = new List<string>();
        string? lastAuthor = null;
        foreach (var item in list.FindAllChildren(cf => cf.ByControlType(ControlType.ListItem)))
        {
            try
            {
                var group = item.FindFirstChild(cf => cf.ByControlType(ControlType.Group));
                var parsed = ParseMessageGroup(group is null ? "" : SafeName(group));
                if (parsed is not null)
                {
                    var (author, text) = parsed.Value;
                    author = string.IsNullOrWhiteSpace(author) ? lastAuthor : author;
                    lastAuthor = author ?? lastAuthor;
                    messages.Add(author is null ? Collapse(text) : $"{Collapse(author)}: {Collapse(text)}");
                    continue;
                }

                var texts = item.FindAllDescendants(cf => cf.ByControlType(ControlType.Text))
                    .Select(SafeName)
                    .Where(t => t.Length > 1)
                    .ToList();
                if (texts.Count > 0)
                {
                    messages.Add(Collapse(string.Join(' ', texts)));
                }
            }
            catch
            {
                // One odd message should not stop the rest.
            }
        }

        return messages;
    }

    /// <summary>"Author , message text , 8:31 PM" -> (Author, message text). Commas inside the text are kept.</summary>
    public static (string Author, string Text)? ParseMessageGroup(string name)
    {
        const string sep = " , ";
        var first = name.IndexOf(sep, StringComparison.Ordinal);
        var last = name.LastIndexOf(sep, StringComparison.Ordinal);
        if (first < 0 || last <= first)
        {
            return null;
        }

        var text = name[(first + sep.Length)..last].Trim();
        return text.Length == 0 ? null : (name[..first].Trim(), text);
    }

    private static string SafeName(AutomationElement element)
    {
        try
        {
            return element.Name ?? "";
        }
        catch
        {
            return "";
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
