using Hangfire;
using WhatsappCrmIA.Application.Interfaces;

namespace WhatsappCrmIA.Api.Services;

public class HangfireReminderScheduler : IReminderScheduler
{
    private readonly IBackgroundJobClient _client;

    public HangfireReminderScheduler(IBackgroundJobClient client) => _client = client;

    public string Schedule(Guid reminderId, DateTime sendAtUtc)
    {
        var delay = sendAtUtc - DateTime.UtcNow;
        if (delay < TimeSpan.Zero) delay = TimeSpan.Zero;

        return _client.Schedule<SendReminderJob>(job => job.ExecuteAsync(reminderId), delay);
    }

    public void Cancel(string jobId) => _client.Delete(jobId);
}
