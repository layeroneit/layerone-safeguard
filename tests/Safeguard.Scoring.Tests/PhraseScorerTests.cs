using Xunit;

namespace LayerOne.Safeguard.Scoring.Tests;

// All lines here are synthetic, written for tests. Never paste real chat into this file.
public class PhraseScorerTests
{
    private static readonly PhraseScorer Scorer = PhraseScorer.CreateDefault();

    [Theory]
    [InlineData("gg that round was close")]
    [InlineData("meet me at spawn")]
    [InlineData("lets go to the obby")]
    [InlineData("send pic")]
    [InlineData("send pic of your base")]
    [InlineData("i need 12 more wins")]
    [InlineData("my mom says dinner in 5")]
    [InlineData("who has the secret sword")]
    [InlineData("delete")]
    [InlineData("")]
    public void Everyday_game_chat_stays_clear(string line)
    {
        Assert.Equal(ScoreLevel.Clear, Scorer.Score(line).Level);
    }

    [Theory]
    [InlineData("what school do you go to", "pii")]
    [InlineData("add me on snap", "migration")]
    [InlineData("dont tell your parents ok", "secrecy")]
    [InlineData("you are so mature for your age", "grooming")]
    [InlineData("we should meet irl", "meeting")]
    [InlineData("what are you wearing", "sexual")]
    public void Each_category_is_found(string line, string category)
    {
        var result = Scorer.Score(line);
        Assert.Contains(result.Hits, h => h.Category.Id == category && !h.Weak);
    }

    [Theory]
    [InlineData("d0nt t3ll ur m0m")]
    [InlineData("DON'T TELL YOUR PARENTS!!!")]
    [InlineData("dont tell ur parents")]
    [InlineData("keep this a secret")]
    [InlineData("our little secret :)")]
    public void Secrecy_survives_leet_shorthand_and_punctuation(string line)
    {
        var result = Scorer.Score(line);
        Assert.Contains(result.Hits, h => h.Category.Id == "secrecy");
        Assert.NotEqual(ScoreLevel.Clear, result.Level);
    }

    [Fact]
    public void Wildcard_allows_a_few_words_between()
    {
        var result = Scorer.Score("send me a cute pic");
        Assert.Contains(result.Hits, h => h.Category.Id == "sexual");
    }

    [Fact]
    public void Wildcard_does_not_span_a_whole_sentence()
    {
        var result = Scorer.Score("send the team to the other side of the map and pic a spot");
        Assert.DoesNotContain(result.Hits, h => h.Category.Id == "sexual");
    }

    [Fact]
    public void Words_must_be_whole()
    {
        // "classic" contains "asl" but is not "asl".
        Assert.DoesNotContain(Scorer.Score("classic mode is best").Hits, h => h.Phrase == "asl");
    }

    [Fact]
    public void Secrecy_plus_meeting_across_lines_is_risk()
    {
        var block = "gg\nwe should meet in person\ndont tell anyone ok";
        var result = Scorer.Score(block);
        Assert.Equal(ScoreLevel.Risk, result.Level);
    }

    [Fact]
    public void Repeating_one_line_does_not_raise_the_level()
    {
        var once = Scorer.Score("add me on snap");
        var many = Scorer.Score(string.Join('\n', Enumerable.Repeat("add me on snap", 20)));
        Assert.Equal(once.Score, many.Score);
    }

    [Fact]
    public void Weak_phrase_alone_is_clear()
    {
        Assert.Equal(ScoreLevel.Clear, Scorer.Score("how old are you").Level);
    }

    [Fact]
    public void Top_hit_is_the_strongest_category()
    {
        var result = Scorer.Score("add me on snap\nwhat are you wearing");
        Assert.Equal("sexual", result.TopHit?.Category.Id);
    }

    [Fact]
    public void Every_category_has_parent_copy()
    {
        foreach (var category in Scorer.Taxonomy.Categories)
        {
            Assert.False(string.IsNullOrWhiteSpace(category.Label), category.Id);
            Assert.False(string.IsNullOrWhiteSpace(category.TalkItOver), category.Id);
            Assert.NotEmpty(category.Phrases);
        }
    }

    [Fact]
    public void Combos_only_name_real_categories()
    {
        var ids = Scorer.Taxonomy.Categories.Select(c => c.Id).ToHashSet();
        foreach (var combo in Scorer.Taxonomy.Combos)
        {
            Assert.All(combo.Categories, id => Assert.Contains(id, ids));
        }
    }
}
