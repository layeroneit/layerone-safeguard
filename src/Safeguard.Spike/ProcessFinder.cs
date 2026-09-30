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

    public static IReadOnlyList<WindowTarget> FindDiscord() => NativeWindows.Find(DiscordNames);

    public static IReadOnlyList<WindowTarget> FindRoblox() => NativeWindows.Find(RobloxNames);
}
