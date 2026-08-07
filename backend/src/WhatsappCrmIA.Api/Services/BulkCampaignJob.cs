using Microsoft.EntityFrameworkCore;
using WhatsappCrmIA.Application.Interfaces;
using WhatsappCrmIA.Domain.Enums;

namespace WhatsappCrmIA.Api.Services;

/// <summary>
/// Roda em segundo plano (Hangfire) e manda a mensagem pra cada destinatário
/// da campanha, um de cada vez, com um intervalo entre cada envio — pra não
/// levar ban do WhatsApp por mandar rápido demais. Confere o status da
/// campanha a cada envio, então dá pra cancelar no meio do caminho.
/// </summary>
public class BulkCampaignJob
{
    private readonly IApplicationDbContext _db;
    private readonly IWhatsAppGateway _whatsApp;
    private readonly ILogger<BulkCampaignJob> _logger;

    public BulkCampaignJob(IApplicationDbContext db, IWhatsAppGateway whatsApp, ILogger<BulkCampaignJob> logger)
    {
        _db = db;
        _whatsApp = whatsApp;
        _logger = logger;
    }

    public async Task ExecuteAsync(Guid campaignId)
    {
        var campaign = await _db.BulkMessageCampaigns
            .IgnoreQueryFilters()
            .Include(c => c.WhatsAppConnection)
            .Include(c => c.Recipients).ThenInclude(r => r.Contact)
            .FirstOrDefaultAsync(c => c.Id == campaignId);

        if (campaign is null)
        {
            _logger.LogWarning("Campanha {CampaignId} não encontrada.", campaignId);
            return;
        }

        if (campaign.Status == BulkCampaignStatus.Cancelled) return;

        campaign.Status = BulkCampaignStatus.Running;
        campaign.StartedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var pendingRecipients = campaign.Recipients
            .Where(r => r.Status == BulkRecipientStatus.Pending)
            .ToList();

        foreach (var recipient in pendingRecipients)
        {
            // Confere se a campanha foi cancelada no meio do caminho.
            var currentStatus = await _db.BulkMessageCampaigns
                .IgnoreQueryFilters()
                .Where(c => c.Id == campaignId)
                .Select(c => c.Status)
                .FirstOrDefaultAsync();

            if (currentStatus == BulkCampaignStatus.Cancelled)
            {
                _logger.LogInformation("Campanha {CampaignId} cancelada, parando o disparo.", campaignId);
                return;
            }

            var message = campaign.MessageText.Replace(
                "{nome}", recipient.Contact.Name ?? recipient.Contact.PhoneNumber);

            try
            {
                await _whatsApp.SendTextMessageAsync(
                    campaign.WhatsAppConnection.InstanceName, recipient.Contact.PhoneNumber, message);

                recipient.Status = BulkRecipientStatus.Sent;
                recipient.SentAtUtc = DateTime.UtcNow;
                campaign.SentCount++;
            }
            catch (Exception ex)
            {
                recipient.Status = BulkRecipientStatus.Failed;
                recipient.ErrorMessage = ex.Message;
                campaign.FailedCount++;
                _logger.LogWarning(ex,
                    "Falha ao enviar campanha {CampaignId} pro contato {ContactId}.", campaignId, recipient.ContactId);
            }

            await _db.SaveChangesAsync();
            await Task.Delay(TimeSpan.FromSeconds(campaign.DelaySeconds));
        }

        campaign.Status = BulkCampaignStatus.Completed;
        campaign.CompletedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Campanha {CampaignId} concluída: {Sent} enviadas, {Failed} falharam.",
            campaignId, campaign.SentCount, campaign.FailedCount);
    }
}
