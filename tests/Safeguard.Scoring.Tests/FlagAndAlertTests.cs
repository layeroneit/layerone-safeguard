using LayerOne.Safeguard.Alerts;
using LayerOne.Safeguard.Core;
using Xunit;

namespace LayerOne.Safeguard.Scoring.Tests;

// Synthetic lines only.
public class FlagAndAlertTests
{
    private static readonly DateTimeOffset T = new(2026, 9, 29, 16, 30, 0, TimeSpan.Zero);

    [Fact]
    public void Clear_reads_are_not_flagged()
    {
        var flagger = new Flagger(PhraseScorer.CreateDefault());
        Assert.Null(flagger.Check("Roblox", "gg\nnice build", T));
    }

    [Fact]
    public void Same_line_on_a_still_screen_flags_once()
    {
        var flagger = new Flagger(PhraseScorer.CreateDefault());
        var screen = "gg\ndont tell your parents";
        Assert.NotNull(flagger.Check("Roblox", screen, T));
        Assert.Null(flagger.Check("Roblox", screen, T.AddSeconds(5)));
        Assert.NotNull(flagger.Check("Roblox", screen + "\nwhat school do you go to", T.AddSeconds(10)));
    }

    [Fact]
    public void Flag_carries_parent_copy_and_context()
    {
        var flagger = new Flagger(PhraseScorer.CreateDefault());
        var item = flagger.Check("Discord", "hey\nwe should meet in person\ndont tell anyone", T)!;
        Assert.True(item.IsRisk);
        Assert.False(string.IsNullOrWhiteSpace(item.TalkItOver));
        Assert.Contains("hey", item.Context);
        Assert.Equal(2, item.Lines.Select(l => l.Category).Distinct().Count());
    }

    [Fact]
    public void Flag_log_round_trips_encrypted_and_prunes()
    {
        var dir = Path.Combine(Path.GetTempPath(), "safeguard-test-" + Guid.NewGuid().ToString("N"));
        try
        {
            var log = new FlagLog(dir, retentionDays: 30);
            log.Append(new FlaggedItem { Time = T, App = "Discord", Label = "meeting in person", Snippet = "meet irl", Context = "meet irl" });
            log.Append(new FlaggedItem { Time = T.AddDays(-45), App = "Roblox", Label = "old", Snippet = "x" });

            var raw = string.Concat(Directory.GetFiles(dir).Select(File.ReadAllText));
            Assert.DoesNotContain("meet irl", raw); // not plain text on disk

            Assert.Equal(2, log.ReadAll().Count);
            Assert.Equal(1, log.Prune(T));
            var left = Assert.Single(log.ReadAll());
            Assert.Equal("meet irl", left.Snippet);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void Alert_copy_includes_talk_it_over_and_urgent_line()
    {
        var copy = AlertCopyWriter.Build(new AlertMessage
        {
            AppName = "roblox",
            Time = T,
            Snippet = "dont tell anyone",
            Category = "keeping secrets from parents",
            TalkItOver = "Let them know they will not be in trouble.",
            Urgent = true
        });

        Assert.StartsWith("Safeguard notice (please look soon):", copy.Subject);
        Assert.Contains("A calm way to bring it up:", copy.Body);
        Assert.Contains("call 911", copy.Body);
        Assert.EndsWith("Layer One IT Consultants LLC", copy.Body);
        Assert.Equal("LayerOne App Safeguard", copy.FromDisplayName);
    }

    [Theory]
    [InlineData("parent@gmail.com", "smtp.gmail.com")]
    [InlineData("Parent@Outlook.com", "smtp-mail.outlook.com")]
    [InlineData("p@icloud.com", "smtp.mail.me.com")]
    public void Known_providers_are_detected(string address, string host)
    {
        Assert.Equal(host, MailProviders.ForAddress(address)?.Host);
    }

    [Fact]
    public void Unknown_provider_returns_null()
    {
        Assert.Null(MailProviders.ForAddress("parent@example.org"));
    }

    [Fact]
    public void Mail_password_is_sealed_in_settings()
    {
        var mail = new MailSettings();
        mail.SetPassword("app-password-123");
        Assert.DoesNotContain("app-password-123", mail.SealedPassword);
        Assert.Equal("app-password-123", mail.TryGetPassword());
        Assert.False(mail.IsReady); // not tested yet
    }
}
