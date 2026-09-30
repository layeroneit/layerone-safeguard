using System.Text.RegularExpressions;

namespace LayerOne.Safeguard.Scoring;

public enum ScoreLevel
{
    Clear,
    Review,
    Risk
}

/// <param name="Line">The line as it was read, for the parent snippet.</param>
/// <param name="Matched">The words that matched, after clean-up (e.g. "dont tell your parents").</param>
public sealed record PhraseHit(Category Category, string Phrase, string Line, bool Weak, string Matched = "")
{
    public int Points => Weak ? 1 : Category.Weight;
}

public sealed record ScoreResult(ScoreLevel Level, int Score, IReadOnlyList<PhraseHit> Hits)
{
    public static readonly ScoreResult Clear = new(ScoreLevel.Clear, 0, Array.Empty<PhraseHit>());

    /// <summary>Strongest hit first; that is what the parent sees as "about".</summary>
    public PhraseHit? TopHit => Hits.OrderByDescending(h => h.Points).FirstOrDefault();
}

/// <summary>
/// Scores one block of chat (one read of a window). On-device only. Never logs text.
/// </summary>
public sealed class PhraseScorer
{
    private readonly Taxonomy _taxonomy;
    private readonly List<(Category Category, string Phrase, Regex Pattern, bool Weak)> _patterns = new();

    public PhraseScorer(Taxonomy taxonomy)
    {
        _taxonomy = taxonomy;
        foreach (var category in taxonomy.Categories)
        {
            foreach (var phrase in category.Phrases)
            {
                _patterns.Add((category, phrase, Compile(phrase), false));
            }

            foreach (var phrase in category.Weak)
            {
                _patterns.Add((category, phrase, Compile(phrase), true));
            }
        }
    }

    public static PhraseScorer CreateDefault() => new(Taxonomy.LoadEmbedded());

    public Taxonomy Taxonomy => _taxonomy;

    public ScoreResult Score(string? block)
    {
        if (string.IsNullOrWhiteSpace(block))
        {
            return ScoreResult.Clear;
        }

        var hits = new List<PhraseHit>();
        foreach (var line in block.Split('\n'))
        {
            var normalized = TextNormalizer.Normalize(line);
            if (normalized.Length == 0)
            {
                continue;
            }

            // Patterns expect every word to end with a space.
            normalized += " ";
            foreach (var (category, phrase, pattern, weak) in _patterns)
            {
                var match = pattern.Match(normalized);
                if (match.Success)
                {
                    hits.Add(new PhraseHit(category, phrase, line.Trim(), weak, match.Value.Trim()));
                }
            }
        }

        if (hits.Count == 0)
        {
            return ScoreResult.Clear;
        }

        // Each category counts once, at its strongest hit, so repeated spam does not inflate the score.
        var perCategory = hits
            .GroupBy(h => h.Category.Id)
            .ToDictionary(g => g.Key, g => g.Max(h => h.Points));

        var score = perCategory.Values.Sum();
        foreach (var combo in _taxonomy.Combos)
        {
            // Weak-only hits do not unlock a combo.
            if (combo.Categories.All(id => perCategory.TryGetValue(id, out var points) && points > 1))
            {
                score += combo.Bonus;
            }
        }

        var level = score >= _taxonomy.Thresholds.Risk ? ScoreLevel.Risk
            : score >= _taxonomy.Thresholds.Review ? ScoreLevel.Review
            : ScoreLevel.Clear;

        return new ScoreResult(level, score, hits);
    }

    private static Regex Compile(string phrase)
    {
        var tokens = phrase
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(token => token == "*"
                ? "(?:\\S+ ){0,3}"
                : "(?:" + string.Join('|', token.Split('|').Select(alt => Regex.Escape(TextNormalizer.Normalize(alt)))) + ") ");

        return new Regex("(?:^| )" + string.Concat(tokens), RegexOptions.Compiled | RegexOptions.CultureInvariant);
    }
}
