namespace LayerOne.Safeguard.Capture;

public sealed record WindowTarget(int ProcessId, string ProcessName, IntPtr Hwnd, string Title, string ClassName);
