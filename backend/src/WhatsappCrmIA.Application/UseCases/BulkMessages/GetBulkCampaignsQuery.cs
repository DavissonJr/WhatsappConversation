using MediatR;
using Microsoft.EntityFrameworkCore;
using WhatsappCrmIA.Application.DTOs;
using WhatsappCrmIA.Application.Interfaces;

namespace WhatsappCrmIA.Application.UseCases.BulkMessages;

public record GetBulkCampaignsQuery : IRequest<IReadOnlyList<BulkCampaignSummaryDto>>;

public class GetBulkCampaignsHandler : IRequestHandler<GetBulkCampaignsQuery, IReadOnlyList<BulkCampaignSummaryDto>>
{
    private readonly IApplicationDbContext _db;
    public GetBulkCampaignsHandler(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<BulkCampaignSummaryDto>> Handle(GetBulkCampaignsQuery request, CancellationToken ct)
    {
        var campaigns = await _db.BulkMessageCampaigns
            .Include(c => c.WhatsAppConnection)
            .OrderByDescending(c => c.CreatedAtUtc)
            .ToListAsync(ct);

        return campaigns.Select(c => new BulkCampaignSummaryDto(
            c.Id, c.Title, c.MessageText, c.WhatsAppConnection.Label, c.Status.ToString(),
            c.DelaySeconds, c.TotalRecipients, c.SentCount, c.FailedCount,
            c.CreatedAtUtc, c.StartedAtUtc, c.CompletedAtUtc)).ToList();
    }
}
