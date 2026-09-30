using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace LayerOne.Safeguard.Core;

/// <summary>
/// Raw log of flagged reads only. One file per day, one encrypted entry per line.
/// Encrypted with DPAPI for the Windows account that runs Safeguard, so another
/// account (or a copied file) cannot read it. Files past the retention window are deleted.
/// </summary>
public sealed class FlagLog
{
    private static readonly byte[] Entropy = "LayerOne.Safeguard.FlagLog.v1"u8.ToArray();
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };
    private readonly object _gate = new();

    public FlagLog(string? directory = null, int retentionDays = 30)
    {
        DirectoryPath = directory ?? Path.Combine(AppSettings.DirectoryPath, "flagged");
        RetentionDays = Math.Max(1, retentionDays);
    }

    public string DirectoryPath { get; }

    public int RetentionDays { get; }

    public void Append(FlaggedItem item)
    {
        var json = JsonSerializer.Serialize(item, JsonOptions);
        var sealedBytes = ProtectedData.Protect(Encoding.UTF8.GetBytes(json), Entropy, DataProtectionScope.CurrentUser);
        lock (_gate)
        {
            Directory.CreateDirectory(DirectoryPath);
            File.AppendAllText(PathFor(item.Time), Convert.ToBase64String(sealedBytes) + Environment.NewLine);
        }
    }

    /// <summary>Newest first. Entries that cannot be decrypted (another account, damage) are skipped.</summary>
    public IReadOnlyList<FlaggedItem> ReadAll()
    {
        var items = new List<FlaggedItem>();
        lock (_gate)
        {
            if (!Directory.Exists(DirectoryPath))
            {
                return items;
            }

            foreach (var file in Directory.GetFiles(DirectoryPath, "*.log"))
            {
                foreach (var line in File.ReadLines(file))
                {
                    if (TryOpen(line, out var item))
                    {
                        items.Add(item);
                    }
                }
            }
        }

        return items.OrderByDescending(i => i.Time).ToList();
    }

    /// <summary>Deletes day files older than the retention window. Returns how many were removed.</summary>
    public int Prune(DateTimeOffset? now = null)
    {
        var cutoff = (now ?? DateTimeOffset.Now).Date.AddDays(-RetentionDays);
        var removed = 0;
        lock (_gate)
        {
            if (!Directory.Exists(DirectoryPath))
            {
                return 0;
            }

            foreach (var file in Directory.GetFiles(DirectoryPath, "*.log"))
            {
                var name = Path.GetFileNameWithoutExtension(file);
                if (DateTime.TryParseExact(name, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var day)
                    && day < cutoff)
                {
                    File.Delete(file);
                    removed++;
                }
            }
        }

        return removed;
    }

    public void DeleteAll()
    {
        lock (_gate)
        {
            if (Directory.Exists(DirectoryPath))
            {
                foreach (var file in Directory.GetFiles(DirectoryPath, "*.log"))
                {
                    File.Delete(file);
                }
            }
        }
    }

    private string PathFor(DateTimeOffset time) =>
        Path.Combine(DirectoryPath, time.ToLocalTime().ToString("yyyy-MM-dd") + ".log");

    private static bool TryOpen(string line, out FlaggedItem item)
    {
        item = null!;
        if (string.IsNullOrWhiteSpace(line))
        {
            return false;
        }

        try
        {
            var plain = ProtectedData.Unprotect(Convert.FromBase64String(line.Trim()), Entropy, DataProtectionScope.CurrentUser);
            item = JsonSerializer.Deserialize<FlaggedItem>(plain)!;
            return item is not null;
        }
        catch
        {
            return false;
        }
    }
}
