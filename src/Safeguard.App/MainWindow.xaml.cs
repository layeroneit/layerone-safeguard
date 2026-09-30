using System.Windows;
using System.Windows.Media;
using LayerOne.Safeguard.Core;

namespace LayerOne.Safeguard.App;

public partial class MainWindow : Window
{
    private readonly AppSettings _settings;
    private readonly MonitorHost _monitor;
    private readonly AlertDispatcher _alerts;
    private Guid? _shownTopId;
    private int _shownCount = -1;
    private bool _allowClose;
    private bool _suppressToggle;

    public MainWindow(AppSettings settings, MonitorHost monitor, AlertDispatcher alerts)
    {
        _settings = settings;
        _monitor = monitor;
        _alerts = alerts;
        InitializeComponent();
        _suppressToggle = true;
        DiscordBox.IsChecked = settings.WatchDiscord;
        RobloxBox.IsChecked = settings.WatchRoblox;
        DiscordNameBox.Text = settings.ChildDiscordUsername ?? "";
        RobloxNameBox.Text = settings.ChildRobloxUsername ?? "";
        _suppressToggle = false;
        RefreshStartupLabel();
        UpdateFrom(_monitor.Snapshot);
    }

    public void UpdateFrom(MonitorSnapshot snapshot)
    {
        HeadlineText.Text = snapshot.Headline;
        DetailText.Text = snapshot.Detail;
        DiscordStatus.Text = snapshot.DiscordStatus;
        RobloxStatus.Text = snapshot.RobloxStatus;
        LastText.Text = string.IsNullOrWhiteSpace(snapshot.LastText)
            ? "Nothing yet. Open Discord or Roblox."
            : snapshot.LastText;
        ToggleWatchButton.Content = snapshot.WatchingEnabled ? "Turn watching off" : "Turn watching on";
        ToggleWatchButton.Background = snapshot.WatchingEnabled
            ? (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#3A2081")!
            : (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFrom("#5C5670")!;
        UpdateNeedsLook(snapshot.NeedsLook);
        UpdateMailStatus();
    }

    private void UpdateNeedsLook(IReadOnlyList<FlaggedItem> items)
    {
        var top = items.Count > 0 ? items[0].Id : (Guid?)null;
        if (top == _shownTopId && items.Count == _shownCount)
        {
            return;
        }

        _shownTopId = top;
        _shownCount = items.Count;
        NeedsLookTitle.Text = items.Count == 0 ? "Needs a look" : $"Needs a look ({items.Count})";
        NeedsLookEmpty.Visibility = items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        ClearNeedsLookButton.Visibility = items.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
        NeedsLookCard.BorderThickness = items.Count == 0 ? new Thickness(0) : new Thickness(2);
        NeedsLookList.ItemsSource = items.Select(NeedsLookRow.From).ToList();
    }

    public void UpdateMailStatus()
    {
        var mail = _settings.Mail;
        if (!mail.IsReady)
        {
            MailStatus.Text = "Not set up yet. Safeguard will list things here, but it cannot email you until you set up email.";
            MailStatus.Foreground = Brush(LayerOne.Safeguard.Brand.Colors.Review);
            MailSetupButton.Content = "Set up";
            return;
        }

        var also = mail.AlsoSendToList();
        var to = also.Count == 0 ? mail.Address : $"{mail.Address} and {string.Join(", ", also)}";
        MailStatus.Text = _alerts.LastProblem is { } problem
            ? $"Sending to {to}. Last email had a problem: {problem}"
            : $"Sending to {to}.";
        MailStatus.Foreground = _alerts.LastProblem is null
            ? Brush(LayerOne.Safeguard.Brand.Colors.SecondaryText)
            : Brush(LayerOne.Safeguard.Brand.Colors.Risk);
        MailSetupButton.Content = "Change";
    }

    private static Brush Brush(string hex) => (Brush)new BrushConverter().ConvertFrom(hex)!;

    private void ClearNeedsLook_Click(object sender, RoutedEventArgs e)
    {
        _monitor.ClearNeedsLook();
        UpdateNeedsLook(Array.Empty<FlaggedItem>());
    }

    private void TestAlert_Click(object sender, RoutedEventArgs e)
    {
        var item = _monitor.RunTestAlert();
        UpdateNeedsLook(_monitor.Snapshot.NeedsLook);
        var where = _settings.Mail.IsReady
            ? $"An email is on its way to {_settings.Mail.Address}."
            : "Email is not set up yet, so no email was sent. Set up email to get one.";
        MessageBox.Show(this,
            item is null
                ? "The test did not flag. Please tell Layer One; this should not happen."
                : $"Test alert sent. It is at the top of Needs a look and in the Flagged log. {where}",
            "Safeguard", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void OpenLog_Click(object sender, RoutedEventArgs e)
    {
        new FlagLogWindow(_monitor.FlagLog) { Owner = this }.ShowDialog();
    }

    private void MailSetup_Click(object sender, RoutedEventArgs e)
    {
        new MailSetupWindow(_settings) { Owner = this }.ShowDialog();
        UpdateMailStatus();
    }

    public void AllowClose() => _allowClose = true;

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        PersistUsernames();
        if (_allowClose)
        {
            return;
        }

        e.Cancel = true;
        Hide();
    }

    private void Username_LostFocus(object sender, RoutedEventArgs e) => PersistUsernames();

    private void PersistUsernames()
    {
        _settings.ChildDiscordUsername = (DiscordNameBox.Text ?? "").Trim();
        _settings.ChildRobloxUsername = (RobloxNameBox.Text ?? "").Trim();
        _settings.Save();
    }

    private void ToggleWatch_Click(object sender, RoutedEventArgs e)
    {
        _settings.WatchingEnabled = !_settings.WatchingEnabled;
        _settings.Save();
        _monitor.Apply(_settings);
        if (!_settings.WatchingEnabled)
        {
            _alerts.OnWatchingTurnedOff();
        }
    }

    private void DiscordBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_suppressToggle)
        {
            return;
        }

        _settings.WatchDiscord = DiscordBox.IsChecked == true;
        _settings.Save();
        _monitor.Apply(_settings);
    }

    private void RobloxBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_suppressToggle)
        {
            return;
        }

        _settings.WatchRoblox = RobloxBox.IsChecked == true;
        _settings.Save();
        _monitor.Apply(_settings);
    }

    private void RefreshStartupLabel()
    {
        StartupLabel.Text = _settings.AutostartRegistered
            ? "Safeguard starts when you sign in to Windows."
            : "Safeguard is running now. If startup could not be added, open Safeguard again after you sign in.";
    }
}

internal sealed record NeedsLookRow(string Title, string When, string Snippet, string Tip, Brush Accent)
{
    private static readonly Brush ReviewBrush = (Brush)new BrushConverter().ConvertFrom(LayerOne.Safeguard.Brand.Colors.Review)!;
    private static readonly Brush RiskBrush = (Brush)new BrushConverter().ConvertFrom(LayerOne.Safeguard.Brand.Colors.Risk)!;

    public static NeedsLookRow From(FlaggedItem item) => new(
        (item.IsTest ? "Test: " : "") + (item.IsRisk ? $"Please look soon: {item.Label}" : char.ToUpper(item.Label[0]) + item.Label[1..]),
        $"{item.App} · {item.Time.ToLocalTime():ddd h:mm tt}",
        $"“{item.Snippet}”   " + string.Join(" ", item.FlaggedWords().Select(w => $"[{w}]")),
        item.TalkItOver,
        item.IsRisk ? RiskBrush : ReviewBrush);
}
