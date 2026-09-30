using LayerOne.Safeguard.Brand;
using LayerOne.Safeguard.Spike;

namespace LayerOne.Safeguard.Spike;

internal static class Program
{
    [STAThread]
    private static int Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.WriteLine($"{Colors.ProductName} — M0 capture spike");
        Console.WriteLine(Colors.Publisher);
        Console.WriteLine("Disclosed · on-device · out-of-process only");
        Console.WriteLine("Setup wizard is M2 — it is not in this spike.");
        Console.WriteLine();

        var discord = DiscordUiaProbe.Run();
        Print("Discord UIA", discord);

        var roblox = RobloxOcrProbe.RunAsync().GetAwaiter().GetResult();
        Print("Roblox Graphics.Capture + OCR", roblox);

        Console.WriteLine();
        if (discord.Status == "SKIP" && roblox.Status == "SKIP")
        {
            Console.WriteLine("Neither target was running. Open Discord and/or Roblox, then run again.");
            return 2;
        }

        if (discord.Status == "FAIL" || roblox.Status == "FAIL")
        {
            Console.WriteLine("One or more live targets failed. That gates M1.");
            return 1;
        }

        Console.WriteLine("Live target(s) returned text. M0 passed for the apps that were open.");
        return 0;
    }

    private static void Print(string title, ProbeResult result)
    {
        Console.WriteLine($"=== {title} [{result.Status}] ===");
        Console.WriteLine(result.Summary);
        if (!string.IsNullOrWhiteSpace(result.Preview))
        {
            Console.WriteLine("--- preview ---");
            Console.WriteLine(result.Preview);
            Console.WriteLine("---------------");
        }

        Console.WriteLine();
    }
}
