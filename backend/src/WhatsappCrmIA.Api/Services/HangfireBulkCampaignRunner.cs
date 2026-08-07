using Hangfire;
using WhatsappCrmIA.Application.Interfaces;

namespace WhatsappCrmIA.Api.Services;

public class HangfireBulkCampaignRunner : IBulkCampaignRunner
{
    private readonly IBackgroundJobClient _client;
    public HangfireBulkCampaignRunner(IBackgroundJobClient client) => _client = client;

    public void Enqueue(Guid campaignId) =>
        _client.Enqueue<BulkCampaignJob>(job => job.ExecuteAsync(campaignId));
}
