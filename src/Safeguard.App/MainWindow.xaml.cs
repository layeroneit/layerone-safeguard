using System.Windows;
using LayerOne.Safeguard.Core;

namespace LayerOne.Safeguard.App;

public partial class MainWindow : Window
{
    private readonly AppSettings _settings;
    private readonly MonitorHost _monitor;
    private bool _allowClose;
    private bool _suppressToggle;

    public MainWindow(AppSettings settings, MonitorHost monitor)
    {
        _settings = settings;
        _monitor = monitor;
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
