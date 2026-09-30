using LayerOne.Safeguard.Capture;

namespace LayerOne.Safeguard.Spike;

internal static class RobloxOcrProbe
{
    public static async Task<ProbeResult> RunAsync()
    {
        var read = await RobloxCapture.TryReadAsync();
        if (read.Summary.Contains("not open", StringComparison.OrdinalIgnoreCase))
        {
            return ProbeResult.Skipped(read.Summary);
        }

        return read.Ok
            ? ProbeResult.Ok(read.Summary, read.Text)
            : ProbeResult.Fail(read.Summary, read.Text);
    }
}
