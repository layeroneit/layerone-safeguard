using System.Diagnostics;

namespace LayerOne.Safeguard.Spike;

internal static class ProcessFinder
{
    internal static readonly string[] DiscordNames =
    [
        "Discord",
        "DiscordPTB",
        "DiscordCanary"
    ];

    internal static readonly string[] RobloxNames =
    [
        "RobloxPlayerBeta",
        "RobloxPlayer"
    ];

    public static Process? FindFirst(IEnumerable<string> names)
    {
        foreach (var name in names)
        {
            var match = Process.GetProcessesByName(name)
                .FirstOrDefault(p => p.MainWindowHandle != IntPtr.Zero);
            if (match is not null)
            {
                return match;
            }
        }

        return Process.GetProcesses()
            .FirstOrDefault(p =>
                names.Any(n => p.ProcessName.Equals(n, StringComparison.OrdinalIgnoreCase))
                && p.MainWindowHandle != IntPtr.Zero);
    }
}
