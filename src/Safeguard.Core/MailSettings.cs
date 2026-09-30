using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;

namespace LayerOne.Safeguard.Core;

/// <summary>
/// The parent's mailbox for alerts. The password is sealed with DPAPI for the
/// Windows account that ran setup, so config.json never holds it in plain text.
/// </summary>
public sealed class MailSettings
{
    private static readonly byte[] Entropy = "LayerOne.Safeguard.Mail.v1"u8.ToArray();

    public const string ViaOutlook = "Outlook";
    public const string ViaSmtp = "Smtp";

    /// <summary>Outlook = hand the email to classic Outlook on this PC (no password stored). Smtp = send directly.</summary>
    public string Method { get; set; } = ViaSmtp;

    public string Address { get; set; } = "";
    public string AlsoSendTo { get; set; } = "";
    public string Host { get; set; } = "";
    public int Port { get; set; } = 587;
    public string SealedPassword { get; set; } = "";

    /// <summary>Set only after a test email went through.</summary>
    public DateTimeOffset? TestedUtc { get; set; }

    /// <summary>Where Safeguard found this mailbox on the Windows account, if it did.</summary>
    public string? FoundOnAccount { get; set; }

    [JsonIgnore]
    public bool UsesOutlook => Method.Equals(ViaOutlook, StringComparison.OrdinalIgnoreCase);

    [JsonIgnore]
    public bool IsReady =>
        TestedUtc is not null
        && !string.IsNullOrWhiteSpace(Address)
        && (UsesOutlook
            || (!string.IsNullOrWhiteSpace(Host) && !string.IsNullOrWhiteSpace(SealedPassword)));

    public IReadOnlyList<string> AlsoSendToList() =>
        AlsoSendTo.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    public void SetPassword(string password)
    {
        var sealedBytes = ProtectedData.Protect(Encoding.UTF8.GetBytes(password), Entropy, DataProtectionScope.CurrentUser);
        SealedPassword = Convert.ToBase64String(sealedBytes);
    }

    public string? TryGetPassword()
    {
        if (string.IsNullOrWhiteSpace(SealedPassword))
        {
            return null;
        }

        try
        {
            var plain = ProtectedData.Unprotect(Convert.FromBase64String(SealedPassword), Entropy, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(plain);
        }
        catch
        {
            return null; // Sealed by another Windows account, or damaged.
        }
    }
}
