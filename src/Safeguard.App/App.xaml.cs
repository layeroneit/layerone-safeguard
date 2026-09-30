using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using H.NotifyIcon;
using LayerOne.Safeguard.Brand;
using LayerOne.Safeguard.Core;

namespace LayerOne.Safeguard.App;

public partial class App : Application
{
    private Mutex? _mutex;
    private TaskbarIcon? _tray;
    private MainWindow? _main;
    private MonitorHost? _monitor;
    private AppSettings? _settings;
    private CancellationTokenSource? _cts;
    private Thread? _worker;
    private DispatcherTimer? _timer;
    private IconFactory.TrayState? _trayState;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _mutex = new Mutex(true, @"Local\LayerOne.Safeguard", out var created);
        if (!created)
        {
            MessageBox.Show(
                "Safeguard is already running. Look for the purple shield in the lower-right corner.",
                Colors.ShortName,
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            Shutdown();
            return;
        }

        _settings = AppSettings.Load();
        var quiet = e.Args.Any(a => a.Equals("--quiet", StringComparison.OrdinalIgnoreCase));

        if (!_settings.AcceptedAsIs)
        {
            var welcome = new WelcomeWindow();
            var ok = welcome.ShowDialog() == true && welcome.Accepted;
            if (!ok)
            {
                Shutdown();
                return;
            }

            _settings.AcceptedAsIs = true;
            _settings.Save();
        }

        var exe = Environment.ProcessPath;
        if (!string.IsNullOrWhiteSpace(exe) && LogonStartup.TryRegister(exe, out _))
        {
            _settings.AutostartRegistered = true;
            _settings.Save();
        }

        _monitor = new MonitorHost(_settings);
        _cts = new CancellationTokenSource();
        _worker = new Thread(() => _monitor.Run(_cts.Token))
        {
            IsBackground = true,
            Name = "Safeguard.Watch"
        };
        _worker.SetApartmentState(ApartmentState.STA);
        _worker.Start();

        _main = new MainWindow(_settings, _monitor);
        BuildTray();

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) =>
        {
            if (_main is null || _monitor is null)
            {
                return;
            }

            var snap = _monitor.Snapshot;
            _main.UpdateFrom(snap);
            if (_tray is not null)
            {
                _tray.ToolTipText = snap.WatchingEnabled
                    ? $"{Colors.ShortName} — {snap.Headline}"
                    : $"{Colors.ShortName} — watching is off";
                var state = IconFactory.StateFor(snap);
                if (state != _trayState)
                {
                    _tray.IconSource = IconFactory.Create(state);
                    _trayState = state;
                }
            }
        };
        _timer.Start();

        if (quiet)
        {
            _main.Hide();
        }
        else
        {
            _main.Show();
        }
    }

    private void BuildTray()
    {
        var menu = new ContextMenu();
        menu.Items.Add(Menu("Open Safeguard", (_, _) => ShowMain()));
        menu.Items.Add(Menu("Turn watching off or on", (_, _) =>
        {
            if (_settings is null || _monitor is null)
            {
                return;
            }

            _settings.WatchingEnabled = !_settings.WatchingEnabled;
            _settings.Save();
            _monitor.Apply(_settings);
        }));
        menu.Items.Add(new Separator());
        menu.Items.Add(Menu("Quit Safeguard", (_, _) => Quit()));

        _tray = new TaskbarIcon
        {
            ToolTipText = Colors.ShortName,
            IconSource = IconFactory.Create(),
            ContextMenu = menu,
            NoLeftClickDelay = true
        };
        _tray.TrayLeftMouseUp += (_, _) => ShowMain();
        _tray.ForceCreate();
    }

    private static MenuItem Menu(string header, RoutedEventHandler click)
    {
        var item = new MenuItem { Header = header };
        item.Click += click;
        return item;
    }

    private void ShowMain()
    {
        if (_main is null)
        {
            return;
        }

        _main.Show();
        _main.WindowState = WindowState.Normal;
        _main.Activate();
    }

    private void Quit()
    {
        if (MessageBox.Show(
                "Stop Safeguard on this computer? It will not watch Discord or Roblox until you open it again.",
                Colors.ShortName,
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) != MessageBoxResult.Yes)
        {
            return;
        }

        _timer?.Stop();
        _cts?.Cancel();
        _main?.AllowClose();
        _main?.Close();
        _tray?.Dispose();
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _timer?.Stop();
        _cts?.Cancel();
        _tray?.Dispose();
        _mutex?.Dispose();
        base.OnExit(e);
    }
}
