using System.Net.Mail;
using System.Windows;
using System.Windows.Media;
using LayerOne.Safeguard.Alerts;
using LayerOne.Safeguard.Core;

namespace LayerOne.Safeguard.App;

public partial class MailSetupWindow : Window
{
    private static readonly Brush Good = (Brush)new BrushConverter().ConvertFrom(LayerOne.Safeguard.Brand.Colors.Clear)!;
    private static readonly Brush Bad = (Brush)new BrushConverter().ConvertFrom(LayerOne.Safeguard.Brand.Colors.Risk)!;
    private static readonly Brush Muted = (Brush)new BrushConverter().ConvertFrom(LayerOne.Safeguard.Brand.Colors.SecondaryText)!;

    private readonly AppSettings _settings;
    private string? _testedFingerprint;
    private bool _loading;
    private bool _outlookOffered;

    public MailSetupWindow(AppSettings settings)
    {
        _settings = settings;
        _loading = true; // XAML defaults raise TextChanged before every control exists.
        InitializeComponent();

        var mail = settings.Mail;
        AddressBox.Text = mail.Address;
        AlsoBox.Text = mail.AlsoSendTo;
        HostBox.Text = mail.Host;
        PortBox.Text = mail.Port.ToString();
        PasswordBox.Password = mail.TryGetPassword() ?? "";
        OutlookRadio.IsChecked = mail.UsesOutlook;
        SmtpRadio.IsChecked = !mail.UsesOutlook;
        _loading = false;

        if (mail.IsReady)
        {
            _testedFingerprint = Fingerprint();
            Show(TestResult, $"Working. Last test went through on {mail.TestedUtc!.Value.ToLocalTime():MMMM d}.", Good);
        }

        Refresh();
    }

    private void Address_Changed(object sender, RoutedEventArgs e)
    {
        if (_loading)
        {
            return;
        }

        var provider = MailProviders.ForAddress(AddressBox.Text);
        if (provider is not null)
        {
            HostBox.Text = provider.Host;
            PortBox.Text = provider.Port.ToString();
        }

        Refresh();
    }

    private void Method_Changed(object sender, RoutedEventArgs e)
    {
        if (!_loading)
        {
            Refresh();
        }
    }

    private bool UseOutlook => OutlookRadio.IsChecked == true && OutlookRadio.IsEnabled;

    private void Input_Changed(object sender, RoutedEventArgs e)
    {
        if (!_loading)
        {
            Refresh();
        }
    }

    private void Refresh()
    {
        var address = AddressBox.Text.Trim();
        var provider = MailProviders.ForAddress(address);
        ProviderText.Text = provider is null
            ? (IsEmail(address) ? "Safeguard does not know this email service yet. Open More settings and type its outgoing mail server." : "")
            : $"{provider.Name} — Safeguard knows how to send with this.";
        PasswordHelp.Text = provider?.PasswordHelp
            ?? "Most email services need an app password here, not your normal password.";

        var found = IsEmail(address) ? MailboxCheck.FindOnThisAccount(address) : null;
        FoundText.Text = !IsEmail(address) ? ""
            : found is not null ? $"Found on {found}."
            : "Not found in Windows or Outlook on this account. That is okay; the test email is what counts.";
        FoundText.Foreground = found is not null ? Good : Muted;

        // Outlook sending only works when Outlook on this account has this mailbox.
        var inOutlook = found is not null && found.StartsWith("Outlook", StringComparison.Ordinal) && OutlookSender.IsInstalled();
        OutlookRadio.IsEnabled = inOutlook;
        OutlookHint.Text = inOutlook
            ? "No password needed. Outlook already has this mailbox. Best for work and Microsoft 365 email."
            : "Only available when this mailbox is set up in Outlook on this computer.";
        if (inOutlook && !_outlookOffered && !_settings.Mail.IsReady)
        {
            _outlookOffered = true;
            OutlookRadio.IsChecked = true; // Suggest the no-password route the first time it is possible.
        }

        if (!inOutlook && OutlookRadio.IsChecked == true)
        {
            SmtpRadio.IsChecked = true;
        }

        var smtpVisibility = UseOutlook ? Visibility.Collapsed : Visibility.Visible;
        PasswordCard.Visibility = smtpVisibility;
        MoreSettings.Visibility = smtpVisibility;
        ProviderText.Visibility = UseOutlook || ProviderText.Text.Length == 0 ? Visibility.Collapsed : Visibility.Visible;
        FoundText.Visibility = FoundText.Text.Length == 0 ? Visibility.Collapsed : Visibility.Visible;

        var ready = IsEmail(address)
                    && AlsoListValid()
                    && (UseOutlook
                        || (PasswordBox.Password.Length > 0
                            && HostBox.Text.Trim().Length > 0
                            && int.TryParse(PortBox.Text, out _)));
        TestButton.IsEnabled = ready;

        // Any change after a good test means test again.
        FinishButton.IsEnabled = _testedFingerprint is not null && _testedFingerprint == Fingerprint();
        if (_testedFingerprint is not null && !FinishButton.IsEnabled && TestResult.Foreground == Good)
        {
            Show(TestResult, "Something changed. Send the test again.", Muted);
        }
    }

    private async void Test_Click(object sender, RoutedEventArgs e)
    {
        TestButton.IsEnabled = false;
        FinishButton.IsEnabled = false;
        Show(TestResult, "Sending…", Muted);

        var address = AddressBox.Text.Trim();
        try
        {
            if (UseOutlook)
            {
                await OutlookSender.SendAsync(address, SplitAlso(), MailSender.TestCopy());
            }
            else
            {
                var account = new MailAccount(address, HostBox.Text.Trim(), int.Parse(PortBox.Text), PasswordBox.Password, SplitAlso());
                await MailSender.SendAsync(account, MailSender.TestCopy());
            }

            _testedFingerprint = Fingerprint();
            Show(TestResult, $"Sent. Check {address} for “Safeguard notice: your test email”. Then press Finish.", Good);
        }
        catch (Exception ex)
        {
            _testedFingerprint = null;
            Show(TestResult, MailSender.Explain(ex), Bad);
        }
        finally
        {
            Refresh();
        }
    }

    private void Finish_Click(object sender, RoutedEventArgs e)
    {
        var mail = _settings.Mail;
        mail.Address = AddressBox.Text.Trim();
        mail.AlsoSendTo = string.Join(", ", SplitAlso());
        mail.Method = UseOutlook ? MailSettings.ViaOutlook : MailSettings.ViaSmtp;
        if (UseOutlook)
        {
            mail.SealedPassword = ""; // Nothing to keep; Outlook signs in on its own.
        }
        else
        {
            mail.Host = HostBox.Text.Trim();
            mail.Port = int.Parse(PortBox.Text);
            mail.SetPassword(PasswordBox.Password);
        }
        mail.TestedUtc = DateTimeOffset.UtcNow;
        mail.FoundOnAccount = MailboxCheck.FindOnThisAccount(mail.Address);
        _settings.MailSetupOffered = true;
        _settings.Save();
        DialogResult = true;
        Close();
    }

    private void Later_Click(object sender, RoutedEventArgs e)
    {
        _settings.MailSetupOffered = true;
        _settings.Save();
        DialogResult = false;
        Close();
    }

    private string Fingerprint() =>
        string.Join('\u001f', UseOutlook ? "outlook" : "smtp", AddressBox.Text.Trim(), HostBox.Text.Trim(), PortBox.Text.Trim(),
            UseOutlook ? "" : PasswordBox.Password, string.Join(',', SplitAlso()));

    private List<string> SplitAlso() =>
        AlsoBox.Text.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

    private bool AlsoListValid() => SplitAlso().All(IsEmail);

    private static bool IsEmail(string value) =>
        MailAddress.TryCreate(value, out var parsed) && parsed.Address.Equals(value, StringComparison.OrdinalIgnoreCase) && value.Contains('.');

    private static void Show(System.Windows.Controls.TextBlock block, string text, Brush brush)
    {
        block.Text = text;
        block.Foreground = brush;
    }
}
