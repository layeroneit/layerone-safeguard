using System.Text;

namespace LayerOne.Safeguard.Scoring;

/// <summary>
/// Turns chat or OCR text into lower-case words so phrases match through
/// leetspeak, shorthand, punctuation, and stretched letters.
/// </summary>
public static class TextNormalizer
{
    private static readonly Dictionary<string, string> Shorthand = new(StringComparer.Ordinal)
    {
        ["u"] = "you",
        ["ya"] = "you",
        ["ur"] = "your",
        ["r"] = "are",
        ["pls"] = "please",
        ["plz"] = "please",
        ["wanna"] = "want to",
        ["2"] = "to",
        ["pix"] = "pics",
        ["sc"] = "snapchat",
        ["ig"] = "insta",
        ["mum"] = "mom",
        ["rents"] = "parents",
        ["convo"] = "chat"
    };

    public static string Normalize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return "";
        }

        var words = new List<string>();
        foreach (var raw in Split(text.ToLowerInvariant()))
        {
            var word = Squeeze(Unleet(raw));
            if (word.Length > 0)
            {
                words.Add(Shorthand.TryGetValue(word, out var full) ? full : word);
            }
        }

        return string.Join(' ', words);
    }

    private static IEnumerable<string> Split(string text)
    {
        var current = new StringBuilder();
        foreach (var ch in text)
        {
            if (ch is '\'' or '’' or '‘')
            {
                continue; // don't -> dont
            }

            if (char.IsLetterOrDigit(ch) || ch is '@' or '$')
            {
                current.Append(ch);
            }
            else if (current.Length > 0)
            {
                yield return current.ToString();
                current.Clear();
            }
        }

        if (current.Length > 0)
        {
            yield return current.ToString();
        }
    }

    // Only rewrite digits inside words that also have letters, so "age 12" stays "12".
    private static string Unleet(string word)
    {
        if (!word.Any(char.IsLetter))
        {
            return word.Replace("@", "").Replace("$", "");
        }

        var builder = new StringBuilder(word.Length);
        foreach (var ch in word)
        {
            builder.Append(ch switch
            {
                '0' => 'o',
                '1' => 'i',
                '3' => 'e',
                '4' => 'a',
                '5' => 's',
                '7' => 't',
                '@' => 'a',
                '$' => 's',
                _ => ch
            });
        }

        return builder.ToString();
    }

    // Runs of 3+ of the same letter collapse to one ("sooooo" -> "so"); doubles stay ("meet").
    private static string Squeeze(string word)
    {
        var builder = new StringBuilder(word.Length);
        for (var i = 0; i < word.Length; i++)
        {
            var run = 1;
            while (i + run < word.Length && word[i + run] == word[i])
            {
                run++;
            }

            builder.Append(word[i]);
            if (run == 2)
            {
                builder.Append(word[i]);
            }

            i += run - 1;
        }

        return builder.ToString();
    }
}
