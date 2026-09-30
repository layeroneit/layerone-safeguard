using System.Text.Json;

namespace LayerOne.Safeguard.Core;

public sealed class AppSettings
{
    public bool AcceptedAsIs { get; set; }
    public bool WatchingEnabled { get; set; } = true;
    public bool WatchDiscord { get; set; } = true;
    public bool WatchRoblox { get; set; } = true;
    public bool AutostartRegistered { get; set; }

    // Optional names the parent typed. Empty = watch the Discord / Roblox app as today.
    // Identity filter (keep only this child's lines) is day 2 — stored only; do not apply yet.
    public string ChildDiscordUsername { get; set; } = "";
    public string ChildRobloxUsername { get; set; } = "";

    // Flagged reads only. Clear reads are never written.
    public int FlagRetentionDays { get; set; } = 30;

    public MailSettings Mail { get; set; } = new();

    /// <summary>First-run email step was shown (finished or skipped).</summary>
    public bool MailSetupOffered { get; set; }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public static string DirectoryPath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LayerOne",
            "Safeguard");

    public static string FilePath => Path.Combine(DirectoryPath, "config.json");

    public static AppSettings Load()
    {
        try
        {
            if (!File.Exists(FilePath))
            {
                return new AppSettings();
            }

            var json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save()
    {
        Directory.CreateDirectory(DirectoryPath);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(this, JsonOptions));
    }
}
