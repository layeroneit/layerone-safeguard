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
        try
        {
            account = FindAccount(outlook.Session, fromAddress)
                ?? throw new InvalidOperationException($"Outlook on this computer does not have {fromAddress} set up.");

            item = outlook.CreateItem(MailItem);
            item.SendUsingAccount = account;
            item.To = string.Join("; ", new[] { fromAddress }.Concat(alsoSendTo));
            item.Subject = copy.Subject;
            item.Body = copy.Body;
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
            Release(item);
            Release(account);
            Release(outlook);
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
