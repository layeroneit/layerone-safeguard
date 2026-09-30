using LayerOne.Safeguard.Capture;
using Xunit;

namespace LayerOne.Safeguard.Scoring.Tests;

// Shapes copied from Discord's accessibility names; the words are synthetic.
public class DiscordReadTests
{
    [Theory]
    [InlineData("KidName , dont tell your parents , 8:30 PM", "KidName", "dont tell your parents")]
    [InlineData("Someone , hi, how are you, ok , 8:31 PM", "Someone", "hi, how are you, ok")]
    public void Message_group_names_split_into_author_and_text(string name, string author, string text)
    {
        var parsed = DiscordCapture.ParseMessageGroup(name);
        Assert.Equal((author, text), parsed);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Just a label")]
    [InlineData("A , 8:30 PM")]
    public void Non_message_names_are_ignored(string name)
    {
        Assert.Null(DiscordCapture.ParseMessageGroup(name));
    }

    [Fact]
    public void A_live_style_discord_read_flags()
    {
        // What the fixed reader hands to scoring: "Author: text", newest last.
        var block = "Friend: gg\nStranger: dont tell ur parents\nStranger: got any pics for me?\nStranger: how old are you";
        var result = PhraseScorer.CreateDefault().Score(block);
        Assert.Equal(ScoreLevel.Risk, result.Level); // secrecy + photo request together
        Assert.Contains(result.Hits, h => h.Category.Id == "sexual" && h.Matched == "pics for me");
    }
}
