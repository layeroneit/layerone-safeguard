namespace LayerOne.Safeguard.Capture;

public sealed record CaptureRead(
    bool Ok,
    string Status,
    string Summary,
    string? Text,
    bool SkippedUnchanged = false)
{
    public static CaptureRead Idle(string summary) =>
        new(true, "Idle", summary, null);

    public static CaptureRead OkText(string summary, string? text) =>
        new(true, "OK", summary, text);

    public static CaptureRead Unchanged(string summary, string? text) =>
        new(true, "OK", summary, text, SkippedUnchanged: true);

    public static CaptureRead Fail(string summary, string? text = null) =>
        new(false, "FAIL", summary, text);
}
