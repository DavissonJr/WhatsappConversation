using WhatsappCrmIA.Domain.Common;
using WhatsappCrmIA.Domain.Enums;

namespace WhatsappCrmIA.Domain.Entities;

/// <summary>
/// Um disparo de mensagem pra vários contatos de uma vez. Roda em segundo
/// plano (Hangfire) com um intervalo entre cada envio — mandar rápido demais
/// é a forma mais comum de tomar ban do WhatsApp.
/// </summary>
public class BulkMessageCampaign : BaseEntity
{
    public string Title { get; set; } = default!;
    public string MessageText { get; set; } = default!; // pode conter {nome}
    public Guid WhatsAppConnectionId { get; set; }
    public WhatsAppConnection WhatsAppConnection { get; set; } = default!;

    public int DelaySeconds { get; set; } = 8;
    public BulkCampaignStatus Status { get; set; } = BulkCampaignStatus.Pending;

    public int TotalRecipients { get; set; }
    public int SentCount { get; set; }
    public int FailedCount { get; set; }

    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }

    public ICollection<BulkMessageRecipient> Recipients { get; set; } = new List<BulkMessageRecipient>();
}

public class BulkMessageRecipient : BaseEntity
{
    public Guid CampaignId { get; set; }
    public BulkMessageCampaign Campaign { get; set; } = default!;

    public Guid ContactId { get; set; }
    public Contact Contact { get; set; } = default!;

    public BulkRecipientStatus Status { get; set; } = BulkRecipientStatus.Pending;
    public DateTime? SentAtUtc { get; set; }
    public string? ErrorMessage { get; set; }
}
