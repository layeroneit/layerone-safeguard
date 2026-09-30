namespace LayerOne.Safeguard.Spike;

internal sealed record ProbeResult(string Status, string Summary, string? Preview)
{
    public bool Passed => Status == "OK";

    public static ProbeResult Ok(string summary, string? preview = null) =>
        new("OK", summary, preview);

    public static ProbeResult Fail(string summary, string? preview = null) =>
        new("FAIL", summary, preview);

    public static ProbeResult Skipped(string summary) =>
        new("SKIP", summary, null);
}
