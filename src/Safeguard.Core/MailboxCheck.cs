using System.Text;
using Microsoft.Win32;

namespace LayerOne.Safeguard.Core;

/// <summary>
/// Looks (read-only) for the parent's address on this Windows account: the
/// Microsoft account used to sign in, and Outlook desktop profiles. A miss is
/// not a failure; the test email is what proves the mailbox works.
/// </summary>
public static class MailboxCheck
{
    public static string? FindOnThisAccount(string? address)
    {
        if (string.IsNullOrWhiteSpace(address) || !address.Contains('@'))
        {
            return null;
        }

        var email = address.Trim();
        try
        {
            if (MicrosoftAccountHas(email))
            {
                return "the Microsoft account signed in to Windows";
            }

            if (OutlookProfileHas(email))
            {
                return "Outlook on this Windows account";
            }
        }
        catch
        {
            // Registry access is best effort.
        }

        return null;
    }

    private static bool MicrosoftAccountHas(string email)
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\IdentityCRL\UserExtendedProperties");
        return key?.GetSubKeyNames().Any(n => n.Equals(email, StringComparison.OrdinalIgnoreCase)) == true;
    }

    private static bool OutlookProfileHas(string email)
    {
        using var office = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Office");
        if (office is null)
        {
            return false;
        }

        var unicode = Encoding.Unicode.GetBytes(email.ToLowerInvariant());
        foreach (var version in office.GetSubKeyNames())
        {
            using var profiles = office.OpenSubKey($@"{version}\Outlook\Profiles");
            if (profiles is not null && Search(profiles, email, unicode, 0))
            {
                return true;
            }
        }

        return false;
    }

    private static bool Search(RegistryKey key, string email, byte[] unicode, int depth)
    {
        if (depth > 4)
        {
            return false;
        }

        foreach (var name in key.GetValueNames())
        {
            switch (key.GetValue(name))
            {
                case string text when text.Contains(email, StringComparison.OrdinalIgnoreCase):
                    return true;
                case byte[] bytes when Contains(Lower(bytes), unicode):
                    return true;
            }
        }

        foreach (var child in key.GetSubKeyNames())
        {
            using var sub = key.OpenSubKey(child);
            if (sub is not null && Search(sub, email, unicode, depth + 1))
            {
                return true;
            }
        }

        return false;
    }

    private static byte[] Lower(byte[] utf16)
    {
        var copy = (byte[])utf16.Clone();
        for (var i = 0; i + 1 < copy.Length; i += 2)
        {
            if (copy[i + 1] == 0 && copy[i] is >= (byte)'A' and <= (byte)'Z')
            {
                copy[i] = (byte)(copy[i] + 32);
            }
        }

        return copy;
    }

    private static bool Contains(byte[] haystack, byte[] needle) =>
        needle.Length > 0 && haystack.AsSpan().IndexOf(needle) >= 0;
}
