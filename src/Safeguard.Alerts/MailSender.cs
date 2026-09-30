using System.Net;
using System.Net.Mail;
using LayerOne.Safeguard.Brand;

namespace LayerOne.Safeguard.Alerts;

/// <summary>The parent's own mailbox. Alerts are sent from it, to it.</summary>
public sealed record MailAccount(
    string Address,
    string Host,
    int Port,
    string Password,
    IReadOnlyList<string> AlsoSendTo);

/// <summary>Known providers so a parent does not have to know server names.</summary>
public sealed record MailProvider(string Name, string Host, int Port, string PasswordHelp);

public static class MailProviders
{
    public static readonly MailProvider Gmail = new(
        "Gmail", "smtp.gmail.com", 587,
        "Gmail needs an app password, not your normal password. Turn on 2-Step Verification, then create one at myaccount.google.com/apppasswords.");

    public static readonly MailProvider Outlook = new(
        "Outlook.com", "smtp-mail.outlook.com", 587,
        "Outlook.com may need an app password. Create one under Microsoft account > Security > Advanced security options.");

    public static readonly MailProvider Yahoo = new(
        "Yahoo Mail", "smtp.mail.yahoo.com", 587,
        "Yahoo needs an app password. Create one under Account Info > Account security > Generate app password.");

    public static readonly MailProvider ICloud = new(
        "iCloud Mail", "smtp.mail.me.com", 587,
        "iCloud needs an app-specific password. Create one at account.apple.com under Sign-In and Security.");

    public static MailProvider? ForAddress(string? address)
    {
        var at = address?.LastIndexOf('@') ?? -1;
        if (at < 0)
        {
            return null;
        }

        var domain = address![(at + 1)..].Trim().ToLowerInvariant();
        return domain switch
        {
            "gmail.com" or "googlemail.com" => Gmail,
            "outlook.com" or "hotmail.com" or "live.com" or "msn.com" => Outlook,
            "yahoo.com" or "ymail.com" => Yahoo,
            "icloud.com" or "me.com" or "mac.com" => ICloud,
            _ => null
        };
    }
}

public static class MailSender
{
    public static async Task SendAsync(MailAccount account, AlertCopy copy, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(account);
        ArgumentNullException.ThrowIfNull(copy);

        using var message = new MailMessage
        {
            From = new MailAddress(account.Address, copy.FromDisplayName),
            Subject = copy.Subject,
            Body = copy.Body,
            IsBodyHtml = false
        };
        message.To.Add(account.Address);
        foreach (var extra in account.AlsoSendTo.Where(a => !string.IsNullOrWhiteSpace(a)))
        {
            message.To.Add(extra.Trim());
        }

        using var client = new SmtpClient(account.Host, account.Port)
        {
            EnableSsl = true,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(account.Address, account.Password),
            Timeout = 20000
        };

        await client.SendMailAsync(message, cancellationToken);
    }

    /// <summary>The test email the setup step sends before it lets the parent finish.</summary>
    public static AlertCopy TestCopy() => new()
    {
        FromDisplayName = Colors.ProductName,
        Subject = $"{Colors.ShortName} notice: your test email",
        Body = string.Join(Environment.NewLine,
            "Hello,",
            "",
            "This is a test from Safeguard. If you can read this, alerts will reach you.",
            "",
            "Safeguard only sends a notice when something in Discord or Roblox may need a look. It does not block or delete anything.",
            "",
            Colors.ShortName,
            Colors.Publisher)
    };

    /// <summary>Sent once when watching is turned off, so a parent knows if it happens.</summary>
    public static AlertCopy TurnedOffCopy(DateTimeOffset when) => new()
    {
        FromDisplayName = Colors.ProductName,
        Subject = $"{Colors.ShortName} notice: watching was turned off",
        Body = string.Join(Environment.NewLine,
            "Hello,",
            "",
            $"Safeguard watching was turned off on this computer on {when.ToLocalTime():dddd, MMMM d 'at' h:mm tt}.",
            "",
            "If you did this, you can ignore this email. If not, open Safeguard from the tray and turn watching back on.",
            "",
            Colors.ShortName,
            Colors.Publisher)
    };

    /// <summary>Turns a mail error into something a parent can act on.</summary>
    public static string Explain(Exception ex)
    {
        var text = (ex.InnerException?.Message ?? ex.Message).ToLowerInvariant();
        if (text.Contains("5.7.0") || text.Contains("5.7.8") || text.Contains("5.7.139") || text.Contains("authentication") || text.Contains("username and password"))
        {
            return "The email service did not accept the password. Most services need an app password here, not your normal one.";
        }

        if (text.Contains("outlook"))
        {
            return ex.InnerException?.Message ?? ex.Message;
        }

        if (text.Contains("protocol violation"))
        {
            return "The mail server and Safeguard could not agree on how to connect. Work and Microsoft 365 addresses usually need \"Use Outlook on this computer\". Otherwise, try port 587.";
        }

        if (text.Contains("timed out") || text.Contains("timeout"))
        {
            return "The email service did not answer in time. Check the internet connection and try again.";
        }

        if (text.Contains("name") && text.Contains("resolve") || text.Contains("no such host"))
        {
            return "Safeguard could not find that email server. Check the server name under More settings.";
        }

        return "The email did not go through: " + (ex.InnerException?.Message ?? ex.Message);
    }
}
