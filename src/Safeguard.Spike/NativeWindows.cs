using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace LayerOne.Safeguard.Spike;

internal sealed record WindowTarget(int ProcessId, string ProcessName, IntPtr Hwnd, string Title, string ClassName);

internal static class NativeWindows
{
    private const int SwRestore = 9;
    private const int MaxTitle = 512;

    public static IReadOnlyList<WindowTarget> Find(IEnumerable<string> processNames)
    {
        var names = new HashSet<string>(processNames, StringComparer.OrdinalIgnoreCase);
        var pids = Process.GetProcesses()
            .Where(p =>
            {
                try
                {
                    return names.Contains(p.ProcessName);
                }
                catch
                {
                    return false;
                }
            })
            .Select(p => p.Id)
            .ToHashSet();

        var found = new List<WindowTarget>();
        NativeEnumWindows((hwnd, lParam) =>
        {
            _ = lParam;
            if (!NativeIsWindowVisible(hwnd) && !NativeIsIconic(hwnd))
            {
                return true;
            }

            NativeGetWindowThreadProcessId(hwnd, out var pid);
            if (!pids.Contains((int)pid))
            {
                return true;
            }

            if (NativeGetWindow(hwnd, 4) != IntPtr.Zero)
            {
                return true;
            }

            var title = GetTitle(hwnd);
            var className = GetClass(hwnd);
            var processName = SafeProcessName((int)pid);
            found.Add(new WindowTarget((int)pid, processName, hwnd, title, className));
            return true;
        }, IntPtr.Zero);

        return found
            .OrderByDescending(w => !NativeIsIconic(w.Hwnd))
            .ThenByDescending(w => w.Title.Length)
            .ToList();
    }

    public static bool TryRestore(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero)
        {
            return false;
        }

        NativeShowWindow(hwnd, SwRestore);
        NativeSetForegroundWindow(hwnd);
        return !NativeIsIconic(hwnd);
    }

    public static bool IsMinimized(IntPtr hwnd) => hwnd != IntPtr.Zero && NativeIsIconic(hwnd);

    private static string SafeProcessName(int pid)
    {
        try
        {
            using var process = Process.GetProcessById(pid);
            return process.ProcessName;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string GetTitle(IntPtr hwnd)
    {
        var builder = new StringBuilder(MaxTitle);
        _ = NativeGetWindowText(hwnd, builder, builder.Capacity);
        return builder.ToString();
    }

    private static string GetClass(IntPtr hwnd)
    {
        var builder = new StringBuilder(256);
        _ = NativeGetClassName(hwnd, builder, builder.Capacity);
        return builder.ToString();
    }

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll", EntryPoint = "EnumWindows")]
    private static extern bool NativeEnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll", EntryPoint = "GetWindowThreadProcessId")]
    private static extern uint NativeGetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll", EntryPoint = "GetWindowTextW", CharSet = CharSet.Unicode)]
    private static extern int NativeGetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", EntryPoint = "GetClassNameW", CharSet = CharSet.Unicode)]
    private static extern int NativeGetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll", EntryPoint = "IsWindowVisible")]
    private static extern bool NativeIsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll", EntryPoint = "IsIconic")]
    private static extern bool NativeIsIconic(IntPtr hWnd);

    [DllImport("user32.dll", EntryPoint = "ShowWindow")]
    private static extern bool NativeShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll", EntryPoint = "SetForegroundWindow")]
    private static extern bool NativeSetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll", EntryPoint = "GetWindow")]
    private static extern IntPtr NativeGetWindow(IntPtr hWnd, uint uCmd);
}
