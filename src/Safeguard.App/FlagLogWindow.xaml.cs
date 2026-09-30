using System.Text;
using System.Windows;
using LayerOne.Safeguard.Core;

namespace LayerOne.Safeguard.App;

/// <summary>
/// Shows the raw flagged log: every flagged read with the matched lines and the
/// full window read around them. Clear reads are never in this log.
/// </summary>
public partial class FlagLogWindow : Window
{
    private readonly FlagLog _log;

    public FlagLogWindow(FlagLog log)
    {
        _log = log;
        InitializeComponent();
        IntroText.Text =
            $"Only reads that needed a look are saved here. Everyday chat is never saved. " +
            $"The log is locked to your Windows sign-in and older entries are deleted after {log.RetentionDays} days.";
        Load();
    }

    private void Load()
    {
        var items = _log.ReadAll();
        if (items.Count == 0)
        {
            LogText.Text = "Nothing has been flagged.";
            return;
        }

        var text = new StringBuilder();
        foreach (var item in items)
        {
            text.AppendLine($"{item.Time.ToLocalTime():yyyy-MM-dd h:mm:ss tt}  {item.App}  {item.Level.ToUpperInvariant()} (score {item.Score}){(item.IsTest ? "  [TEST]" : "")}");
            text.AppendLine($"About: {item.Label}");
            foreach (var line in item.Lines)
            {
                text.AppendLine($"  [{line.Category}] {line.Line}");
                text.AppendLine($"      flagged: [{(string.IsNullOrWhiteSpace(line.Matched) ? line.Phrase : line.Matched)}]  (rule: {line.Phrase})");
            }

            text.AppendLine("  What was on screen:");
            foreach (var context in item.Context.Split('\n'))
            {
                text.AppendLine("    " + context.TrimEnd());
            }

            text.AppendLine(new string('─', 60));
        }

        LogText.Text = text.ToString();
    }

    private void DeleteAll_Click(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show(this, "Delete every entry in the flagged log? This cannot be undone.",
                "Safeguard", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
        {
            return;
        }

        _log.DeleteAll();
        Load();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
