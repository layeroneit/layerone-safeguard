using LayerOne.Safeguard.Capture;

namespace LayerOne.Safeguard.Spike;

internal static class DiscordUiaProbe
{
    public static ProbeResult Run()
    {
        var read = DiscordCapture.TryRead();
        if (read.Summary.Contains("not open", StringComparison.OrdinalIgnoreCase))
        {
            return ProbeResult.Skipped(read.Summary);
        }

        return read.Ok
            ? ProbeResult.Ok(read.Summary, read.Text)
            : ProbeResult.Fail(read.Summary, read.Text);
    }
}
