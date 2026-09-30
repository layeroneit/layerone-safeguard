namespace LayerOne.Safeguard.Core;

public sealed class MonitorSnapshot
{
    public bool WatchingEnabled { get; init; }
    public bool DiscordOpen { get; init; }
    public bool RobloxOpen { get; init; }
    public bool WatchDiscord { get; init; }
    public bool WatchRoblox { get; init; }
    public string Headline { get; init; } = "Safeguard is resting.";
    public string Detail { get; init; } = "It wakes up only when Discord or Roblox is open.";
    public string DiscordStatus { get; init; } = "Discord is not open.";
    public string RobloxStatus { get; init; } = "Roblox is not open.";
    public string? LastText { get; init; }

    /// <summary>Lines scored as "needs a look" and not yet dismissed by the parent.</summary>
    public int NeedsLookCount { get; init; }
    public DateTimeOffset UpdatedUtc { get; init; } = DateTimeOffset.UtcNow;
}
