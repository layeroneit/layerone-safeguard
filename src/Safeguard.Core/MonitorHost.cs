using LayerOne.Safeguard.Capture;

namespace LayerOne.Safeguard.Core;

public sealed class MonitorHost
{
    private readonly object _gate = new();
    private AppSettings _settings;
    private MonitorSnapshot _snapshot;
    private DateTimeOffset _nextDiscord = DateTimeOffset.MinValue;
    private DateTimeOffset _nextRoblox = DateTimeOffset.MinValue;

    public MonitorHost(AppSettings settings)
    {
        _settings = settings;
        _snapshot = BuildIdle("Safeguard is starting…", "Getting ready.");
    }

    public MonitorSnapshot Snapshot
    {
        get
        {
            lock (_gate)
            {
                return _snapshot;
            }
        }
    }

    public void Apply(AppSettings settings)
    {
        lock (_gate)
        {
            _settings = settings;
        }
    }

    public void Run(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                Tick();
            }
            catch
            {
                // Stay alive. Parents should not see a crash from one failed read.
            }

            var idle = !Snapshot.DiscordOpen && !Snapshot.RobloxOpen;
            var delay = idle ? 2500 : 1500;
            cancellationToken.WaitHandle.WaitOne(delay);
        }
    }

    private void Tick()
    {
        AppSettings settings;
        lock (_gate)
        {
            settings = _settings;
        }

        var discordOpen = NativeWindows.AnyProcess(NativeWindows.DiscordProcessNames);
        var robloxOpen = NativeWindows.AnyProcess(NativeWindows.RobloxProcessNames);
        var now = DateTimeOffset.UtcNow;

        var discordStatus = "Discord is not open.";
        var robloxStatus = "Roblox is not open.";
        string? lastText = Snapshot.LastText;
        var discordRead = CaptureRead.Idle(discordStatus);
        var robloxRead = CaptureRead.Idle(robloxStatus);

        if (!settings.WatchingEnabled)
        {
            Publish(new MonitorSnapshot
            {
                WatchingEnabled = false,
                DiscordOpen = discordOpen,
                RobloxOpen = robloxOpen,
                WatchDiscord = settings.WatchDiscord,
                WatchRoblox = settings.WatchRoblox,
                Headline = "Watching is turned off.",
                Detail = "Safeguard is still in the tray. Turn watching on when you want it to read Discord or Roblox.",
                DiscordStatus = discordOpen ? "Discord is open, but watching is off." : discordStatus,
                RobloxStatus = robloxOpen ? "Roblox is open, but watching is off." : robloxStatus,
                LastText = null,
                UpdatedUtc = now
            });
            return;
        }

        if (settings.WatchDiscord && discordOpen && now >= _nextDiscord)
        {
            discordRead = DiscordCapture.TryRead();
            _nextDiscord = now.AddSeconds(4);
            discordStatus = discordRead.Summary;
            if (!string.IsNullOrWhiteSpace(discordRead.Text))
            {
                lastText = discordRead.Text;
            }
        }
        else if (settings.WatchDiscord && discordOpen)
        {
            discordStatus = Snapshot.DiscordStatus;
        }
        else if (!settings.WatchDiscord)
        {
            discordStatus = discordOpen ? "Discord is open. Watching Discord is turned off." : discordStatus;
        }

        if (settings.WatchRoblox && robloxOpen && now >= _nextRoblox)
        {
            robloxRead = RobloxCapture.TryReadAsync().GetAwaiter().GetResult();
            _nextRoblox = robloxRead.SkippedUnchanged ? now.AddSeconds(6) : now.AddSeconds(5);
            robloxStatus = robloxRead.Summary;
            if (!string.IsNullOrWhiteSpace(robloxRead.Text) && !robloxRead.SkippedUnchanged)
            {
                lastText = robloxRead.Text;
            }
        }
        else if (settings.WatchRoblox && robloxOpen)
        {
            robloxStatus = Snapshot.RobloxStatus;
        }
        else if (!settings.WatchRoblox)
        {
            robloxStatus = robloxOpen ? "Roblox is open. Watching Roblox is turned off." : robloxStatus;
        }

        var anyWatchedOpen =
            (settings.WatchDiscord && discordOpen) || (settings.WatchRoblox && robloxOpen);

        Publish(new MonitorSnapshot
        {
            WatchingEnabled = true,
            DiscordOpen = discordOpen,
            RobloxOpen = robloxOpen,
            WatchDiscord = settings.WatchDiscord,
            WatchRoblox = settings.WatchRoblox,
            Headline = anyWatchedOpen ? "Safeguard is watching." : "Safeguard is resting.",
            Detail = anyWatchedOpen
                ? "It only reads when Discord or Roblox is open. It does not change those apps."
                : "Discord and Roblox are closed. Safeguard is using almost no work.",
            DiscordStatus = discordStatus,
            RobloxStatus = robloxStatus,
            LastText = lastText,
            UpdatedUtc = now
        });
    }

    private void Publish(MonitorSnapshot snapshot)
    {
        lock (_gate)
        {
            _snapshot = snapshot;
        }
    }

    private static MonitorSnapshot BuildIdle(string headline, string detail) =>
        new()
        {
            Headline = headline,
            Detail = detail
        };
}
