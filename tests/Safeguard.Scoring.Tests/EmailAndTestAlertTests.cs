using LayerOne.Safeguard.Alerts;
using LayerOne.Safeguard.Core;
using Xunit;

namespace LayerOne.Safeguard.Scoring.Tests;

// Synthetic lines only.
public class EmailAndTestAlertTests
{
    private static readonly DateTimeOffset T = new(2026, 9, 29, 16, 30, 0, TimeSpan.Zero);

    [Fact]
    public void Test_line_flags_as_secrecy_with_its_words()
    {
        var result = PhraseScorer.CreateDefault().Score(MonitorHost.TestLine);
        Assert.Equal(ScoreLevel.Review, result.Level);
        var hit = Assert.Single(result.Hits);
        Assert.Equal("secrecy", hit.Category.Id);
        Assert.Equal("dont tell your parents", hit.Matched);
    }

    [Fact]
    public void Test_alerts_repeat_but_real_lines_do_not()
    {
        var flagger = new Flagger(PhraseScorer.CreateDefault());
        Assert.NotNull(flagger.Check("Safeguard test", MonitorHost.TestLine, T, isTest: true));
        Assert.NotNull(flagger.Check("Safeguard test", MonitorHost.TestLine, T, isTest: true));
        Assert.NotNull(flagger.Check("Discord", MonitorHost.TestLine, T));
        Assert.Null(flagger.Check("Discord", MonitorHost.TestLine, T));
    }

    [Fact]
    public void Alert_email_shows_flagged_words_and_logo()
    {
        var copy = AlertCopyWriter.Build(new AlertMessage
        {
            AppName = "Discord",
            Time = T,
            Snippet = "dont tell ur parents",
            Category = "keeping secrets from parents",
            FlaggedWords = new[] { "dont tell your parents" }
        });

        Assert.Contains("Flagged words: [dont tell your parents]", copy.Body);
        Assert.Contains("[dont tell your parents]", copy.HtmlBody);
        Assert.Contains($"cid:{EmailBuilder.LogoContentId}", copy.HtmlBody);
        Assert.Contains("Layer One IT Consultants LLC", copy.HtmlBody);
        Assert.NotEmpty(EmailBuilder.LogoPng());
    }

    [Fact]
    public void Html_escapes_chat_text()
    {
        var copy = AlertCopyWriter.Build(new AlertMessage { AppName = "Discord", Time = T, Snippet = "<script>x</script>" });
        Assert.DoesNotContain("<script>", copy.HtmlBody);
    }

    [Fact]
    public void Test_alert_is_labelled_as_a_test()
    {
        var copy = AlertCopyWriter.Build(new AlertMessage { AppName = "Safeguard test", Time = T, Snippet = "x", IsTest = true });
        Assert.StartsWith("Safeguard notice (test):", copy.Subject);
        Assert.Contains("This is a test alert", copy.Body);
    }
}
