using System.Windows.Media;
using System.Windows.Media.Imaging;
using LayerOne.Safeguard.Core;

namespace LayerOne.Safeguard.App;

/// <summary>
/// Tray icons built by tools/make_icons.py. One per state so a parent (and the
/// child) can tell at a glance what Safeguard is doing.
/// </summary>
internal static class IconFactory
{
    public enum TrayState
    {
        Resting,
        Watching,
        Review,
        Off
    }

    private static readonly Dictionary<TrayState, ImageSource> Cache = new();

    public static TrayState StateFor(MonitorSnapshot snapshot)
    {
        if (!snapshot.WatchingEnabled)
        {
            return TrayState.Off;
        }

        if (snapshot.NeedsLookCount > 0)
        {
            return TrayState.Review;
        }

        var watching = (snapshot.WatchDiscord && snapshot.DiscordOpen)
                       || (snapshot.WatchRoblox && snapshot.RobloxOpen);
        return watching ? TrayState.Watching : TrayState.Resting;
    }

    public static ImageSource Create(TrayState state = TrayState.Resting)
    {
        if (Cache.TryGetValue(state, out var cached))
        {
            return cached;
        }

        var name = state.ToString().ToLowerInvariant();
        var image = new BitmapImage(new Uri($"pack://application:,,,/Assets/tray/{name}.ico"));
        image.Freeze();
        Cache[state] = image;
        return image;
    }
}
