using LayerOne.Safeguard.Scoring;

namespace LayerOne.Safeguard.Core;

/// <summary>
/// Scores each chat read and turns new "needs a look" reads into FlaggedItems.
/// Remembers lines it already flagged so a still Roblox screen or a Discord
/// channel that has not scrolled does not flag the same line again.
/// </summary>
public sealed class Flagger
{
    private const int MemoryLimit = 2000;
    private readonly PhraseScorer _scorer;
    private readonly HashSet<string> _seen = new(StringComparer.Ordinal);
    private readonly Queue<string> _order = new();

    public Flagger(PhraseScorer scorer)
    {
        _scorer = scorer;
    }

    public FlaggedItem? Check(string app, string? text, DateTimeOffset time)
    {
        var result = _scorer.Score(text);
        if (result.Level == ScoreLevel.Clear)
        {
            return null;
        }

        // Only new flagged lines count. If every hit line was flagged before, stay quiet.
        var fresh = result.Hits
            .Select(h => app + "|" + TextNormalizer.Normalize(h.Line))
            .Distinct()
            .Where(key => !_seen.Contains(key))
            .ToList();
        if (fresh.Count == 0)
        {
            return null;
        }

        foreach (var key in fresh)
        {
            Remember(key);
        }

        var top = result.TopHit!;
        return new FlaggedItem
        {
            Time = time,
            App = app,
            Level = result.Level.ToString(),
            Score = result.Score,
            Label = top.Category.Label,
            Snippet = top.Line,
            TalkItOver = top.Category.TalkItOver,
            Lines = result.Hits.Select(h => new FlaggedLine(h.Category.Id, h.Phrase, h.Line)).ToList(),
            Context = text ?? ""
        };
    }

    private void Remember(string key)
    {
        if (_seen.Add(key))
        {
            _order.Enqueue(key);
        }

        while (_order.Count > MemoryLimit)
        {
            _seen.Remove(_order.Dequeue());
        }
    }
}
