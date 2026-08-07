using MediatR;
using Microsoft.EntityFrameworkCore;
using WhatsappCrmIA.Application.Interfaces;
using WhatsappCrmIA.Domain.Enums;

namespace WhatsappCrmIA.Application.UseCases.BulkMessages;

public record CancelBulkCampaignCommand(Guid CampaignId) : IRequest<bool>;

public class CancelBulkCampaignHandler : IRequestHandler<CancelBulkCampaignCommand, bool>
{
    private readonly IApplicationDbContext _db;
    public CancelBulkCampaignHandler(IApplicationDbContext db) => _db = db;

    public async Task<bool> Handle(CancelBulkCampaignCommand request, CancellationToken ct)
    {
        var campaign = await _db.BulkMessageCampaigns.FirstOrDefaultAsync(c => c.Id == request.CampaignId, ct);
        if (campaign is null) return false;
        if (campaign.Status is not (BulkCampaignStatus.Pending or BulkCampaignStatus.Running)) return false;

        // O job em segundo plano confere esse status a cada envio e para
        // sozinho assim que perceber a mudança — não precisa matar a thread.
        campaign.Status = BulkCampaignStatus.Cancelled;
        campaign.CompletedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
