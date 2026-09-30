using LayerOne.Safeguard.Alerts;
using LayerOne.Safeguard.Core;

namespace LayerOne.Safeguard.App;

/// <summary>
/// Emails the parent about flagged reads. Same app + same topic waits 30 minutes
/// before a second email; "please look soon" reads always go out. At most 10 an hour.
/// </summary>
public sealed class AlertDispatcher
{
    private static readonly TimeSpan SameTopicCooldown = TimeSpan.FromMinutes(30);
    private const int HourlyLimit = 10;

    private readonly AppSettings _settings;
    private readonly object _gate = new();
    private readonly Dictionary<string, DateTimeOffset> _lastByTopic = new();
    private readonly Queue<DateTimeOffset> _recent = new();

    public AlertDispatcher(AppSettings settings)
    {
        _settings = settings;
    }

    /// <summary>Last send problem in parent words, or null when the last send worked.</summary>
    public string? LastProblem { get; private set; }

    public void OnFlagged(FlaggedItem item)
    {
        if (!_settings.Mail.IsReady || !ShouldSend(item))
        {
            return;
        }

        var copy = AlertCopyWriter.Build(new AlertMessage
        {
            AppName = item.App,
            Time = item.Time,
            Snippet = item.Snippet,
            Category = item.Label,
            TalkItOver = item.TalkItOver,
            Urgent = item.IsRisk
        });
        _ = SendAsync(copy);
    }

    public void OnWatchingTurnedOff()
    {
        if (_settings.Mail.IsReady)
        {
            _ = SendAsync(MailSender.TurnedOffCopy(DateTimeOffset.Now));
        }
    }

    private bool ShouldSend(FlaggedItem item)
    {
        var now = DateTimeOffset.UtcNow;
        lock (_gate)
        {
            while (_recent.Count > 0 && now - _recent.Peek() > TimeSpan.FromHours(1))
            {
                _recent.Dequeue();
            }

            if (_recent.Count >= HourlyLimit)
            {
                return false;
            }

            var topic = item.App + "|" + item.Label;
            if (!item.IsRisk && _lastByTopic.TryGetValue(topic, out var last) && now - last < SameTopicCooldown)
            {
                return false;
            }

            _lastByTopic[topic] = now;
            _recent.Enqueue(now);
            return true;
        }
    }

    private async Task SendAsync(AlertCopy copy)
    {
        var mail = _settings.Mail;
        var password = mail.TryGetPassword();
        if (password is null)
        {
            LastProblem = "Safeguard could not unlock the email password. Open Email alerts and set it up again.";
            return;
        }

        try
        {
            await MailSender.SendAsync(new MailAccount(mail.Address, mail.Host, mail.Port, password, mail.AlsoSendToList()), copy);
            LastProblem = null;
        }
        catch (Exception ex)
        {
            LastProblem = MailSender.Explain(ex);
        }
    }
}
