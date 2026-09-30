using System.Diagnostics;

namespace LayerOne.Safeguard.Core;

public static class LogonStartup
{
    public const string TaskName = "LayerOne Safeguard";

    public static bool TryRegister(string exePath, out string message)
    {
        if (string.IsNullOrWhiteSpace(exePath) || !File.Exists(exePath))
        {
            message = "Safeguard could not find its own program file.";
            return false;
        }

        var command = $"\\\"{exePath}\\\" --quiet";
        var args = $"/Create /TN \"{TaskName}\" /TR \"{command}\" /SC ONLOGON /RL LIMITED /F";
        var result = RunSchtasks(args, out var output);
        if (!result)
        {
            message = string.IsNullOrWhiteSpace(output)
                ? "Windows would not add Safeguard to startup."
                : output.Trim();
            return false;
        }

        message = "Safeguard will start when you sign in to Windows.";
        return true;
    }

    public static bool IsRegistered()
    {
        return RunSchtasks($"/Query /TN \"{TaskName}\"", out _);
    }

    public static bool TryRemove(out string message)
    {
        if (!IsRegistered())
        {
            message = "Startup was already off.";
            return true;
        }

        var ok = RunSchtasks($"/Delete /TN \"{TaskName}\" /F", out var output);
        message = ok ? "Safeguard will not start at sign-in." : output.Trim();
        return ok;
    }

    private static bool RunSchtasks(string arguments, out string output)
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "schtasks.exe",
                Arguments = arguments,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            });

            if (process is null)
            {
                output = "Could not start the Windows task helper.";
                return false;
            }

            var std = process.StandardOutput.ReadToEnd();
            var err = process.StandardError.ReadToEnd();
            process.WaitForExit(8000);
            output = string.IsNullOrWhiteSpace(err) ? std : err;
            return process.ExitCode == 0;
        }
        catch (Exception ex)
        {
            output = ex.Message;
            return false;
        }
    }
}
