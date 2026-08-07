using MediatR;
using Microsoft.EntityFrameworkCore;
using WhatsappCrmIA.Application.DTOs;
using WhatsappCrmIA.Application.Interfaces;
using WhatsappCrmIA.Domain.Entities;
using WhatsappCrmIA.Domain.Enums;

namespace WhatsappCrmIA.Application.UseCases.BulkMessages;

public record CreateBulkCampaignCommand(
    string Title,
    string MessageText,
    Guid WhatsAppConnectionId,
    int DelaySeconds,
    BulkAudienceFilters Filters
) : IRequest<(Guid? CampaignId, string? Error)>;

public class CreateBulkCampaignHandler : IRequestHandler<CreateBulkCampaignCommand, (Guid? CampaignId, string? Error)>
{
    // Nunca deixa mandar rápido demais — é a forma mais comum de tomar ban
    // do WhatsApp em disparos em massa.
    private const int MinDelaySeconds = 3;

    private readonly IApplicationDbContext _db;
    private readonly IBulkCampaignRunner _runner;

    public CreateBulkCampaignHandler(IApplicationDbContext db, IBulkCampaignRunner runner)
    {
        _db = db;
        _runner = runner;
    }

    public async Task<(Guid? CampaignId, string? Error)> Handle(CreateBulkCampaignCommand request, CancellationToken ct)
    {
        var connection = await _db.WhatsAppConnections
            .FirstOrDefaultAsync(w => w.Id == request.WhatsAppConnectionId, ct);
        if (connection is null) return (null, "Número de WhatsApp inválido.");

        var contacts = await BulkAudienceResolver.ResolveAsync(_db, request.Filters, ct);
        if (contacts.Count == 0) return (null, "Nenhum contato encontrado com esses filtros.");

        var campaign = new BulkMessageCampaign
        {
            Title = request.Title,
            MessageText = request.MessageText,
            WhatsAppConnectionId = connection.Id,
            DelaySeconds = Math.Max(MinDelaySeconds, request.DelaySeconds),
            Status = BulkCampaignStatus.Pending,
            TotalRecipients = contacts.Count
        };
        _db.BulkMessageCampaigns.Add(campaign);

        foreach (var contact in contacts)
        {
            _db.BulkMessageRecipients.Add(new BulkMessageRecipient
            {
                Campaign = campaign,
                ContactId = contact.Id,
                Status = BulkRecipientStatus.Pending
            });
        }

        await _db.SaveChangesAsync(ct);

        _runner.Enqueue(campaign.Id);

        return (campaign.Id, null);
    }
}
