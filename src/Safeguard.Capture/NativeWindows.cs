using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace LayerOne.Safeguard.Capture;

public static class NativeWindows
{
    private const int MaxTitle = 512;

    public static readonly string[] DiscordProcessNames =
    [
        "Discord",
        "DiscordPTB",
        "DiscordCanary"
    ];

    public static readonly string[] RobloxProcessNames =
    [
        "RobloxPlayerBeta",
        "RobloxPlayer"
    ];

    public static IReadOnlyList<WindowTarget> FindDiscord() => Find(DiscordProcessNames);

    public static IReadOnlyList<WindowTarget> FindRoblox() => Find(RobloxProcessNames);

    public static bool AnyProcess(IEnumerable<string> processNames)
    {
        var names = new HashSet<string>(processNames, StringComparer.OrdinalIgnoreCase);
        return Process.GetProcesses().Any(p =>
        {
            try
            {
                return names.Contains(p.ProcessName);
            }
            catch
            {
                return false;
            }
        });
    }

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

            found.Add(new WindowTarget(
                (int)pid,
                SafeProcessName((int)pid),
                hwnd,
                GetTitle(hwnd),
                GetClass(hwnd)));
            return true;
        }, IntPtr.Zero);

        return found
            .OrderByDescending(w => !NativeIsIconic(w.Hwnd))
            .ThenByDescending(w => w.Title.Length)
            .ToList();
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

    [DllImport("user32.dll", EntryPoint = "GetWindow")]
    private static extern IntPtr NativeGetWindow(IntPtr hWnd, uint uCmd);
}
