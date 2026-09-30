using System.Runtime.InteropServices;

namespace LayerOne.Safeguard.Alerts;

/// <summary>
/// Sends through classic Outlook on this Windows account (COM). No password is
/// stored; Outlook already signs in to the mailbox, including Microsoft 365
/// mailboxes where password-based SMTP is turned off.
/// </summary>
public static class OutlookSender
{
    private const int MailItem = 0;
    private const int OlFormatHtml = 2;
    private const int OlByValue = 1;
    private const string PropContentId = "http://schemas.microsoft.com/mapi/proptag/0x3712001F";
    private const string PropHidden = "http://schemas.microsoft.com/mapi/proptag/0x7FFE000B";

    public static bool IsInstalled() => Type.GetTypeFromProgID("Outlook.Application") is not null;

    public static Task SendAsync(string fromAddress, IReadOnlyList<string> alsoSendTo, AlertCopy copy)
    {
        var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try
            {
                Send(fromAddress, alsoSendTo, copy);
                done.SetResult();
            }
            catch (Exception ex)
            {
                done.SetException(ex);
            }
        })
        {
            IsBackground = true,
            Name = "Safeguard.Outlook"
        };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return done.Task;
    }

    private static void Send(string fromAddress, IReadOnlyList<string> alsoSendTo, AlertCopy copy)
    {
        var type = Type.GetTypeFromProgID("Outlook.Application")
            ?? throw new InvalidOperationException("Outlook is not installed on this computer.");

        dynamic outlook = Activator.CreateInstance(type)!;
        dynamic? account = null;
        dynamic? item = null;
        string? logoPath = null;
        try
        {
            account = FindAccount(outlook.Session, fromAddress)
                ?? throw new InvalidOperationException($"Outlook on this computer does not have {fromAddress} set up.");

            item = outlook.CreateItem(MailItem);
            item.SendUsingAccount = account;
            item.To = string.Join("; ", new[] { fromAddress }.Concat(alsoSendTo));
            item.Subject = copy.Subject;
            if (string.IsNullOrWhiteSpace(copy.HtmlBody))
            {
                item.Body = copy.Body;
            }
            else
            {
                logoPath = AttachInlineLogo(item);
                item.BodyFormat = OlFormatHtml;
                item.HTMLBody = copy.HtmlBody;
            }

            item.Send();

            // Nudge Outlook to send now rather than at its next sync.
            try
            {
                outlook.Session.SendAndReceive(false);
            }
            catch
            {
                // Outlook sends on its own schedule if this is not allowed.
            }
        }
        finally
        {
            TryDelete(logoPath);
            Release(item);
            Release(account);
            Release(outlook);
        }
    }

    // Outlook needs a file to attach; the content id lets the HTML show it inline.
    private static string AttachInlineLogo(dynamic item)
    {
        var path = Path.Combine(Path.GetTempPath(), $"safeguard-logo-{Guid.NewGuid():N}.png");
        File.WriteAllBytes(path, EmailBuilder.LogoPng());
        dynamic attachment = item.Attachments.Add(path, OlByValue, 0, "safeguard.png");
        try
        {
            dynamic props = attachment.PropertyAccessor;
            props.SetProperty(PropContentId, EmailBuilder.LogoContentId);
            props.SetProperty(PropHidden, true);
            Release(props);
        }
        finally
        {
            Release(attachment);
        }

        return path;
    }

    private static void TryDelete(string? path)
    {
        try
        {
            if (path is not null && File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Temp file; Windows cleans it up eventually.
        }
    }

    private static dynamic? FindAccount(dynamic session, string address)
    {
        dynamic accounts = session.Accounts;
        int count = accounts.Count;
        for (var i = 1; i <= count; i++)
        {
            dynamic candidate = accounts.Item(i);
            string smtp = candidate.SmtpAddress ?? "";
            if (smtp.Equals(address, StringComparison.OrdinalIgnoreCase))
            {
                return candidate;
            }

            Release(candidate);
        }

        return null;
    }

    private static void Release(object? com)
    {
        if (com is not null && Marshal.IsComObject(com))
        {
            Marshal.ReleaseComObject(com);
        }
    }
}
