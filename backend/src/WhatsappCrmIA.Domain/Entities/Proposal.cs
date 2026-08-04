using WhatsappCrmIA.Domain.Common;
using WhatsappCrmIA.Domain.Enums;

namespace WhatsappCrmIA.Domain.Entities;

/// <summary>
/// Proposta gerada (pela IA ou manualmente) a partir de uma conversa.
/// </summary>
public class Proposal : BaseEntity
{
    public Guid ContactId { get; set; }
    public Contact Contact { get; set; } = default!;

    public Guid? ConversationId { get; set; }

    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public decimal? Value { get; set; }
    public ProposalStatus Status { get; set; } = ProposalStatus.Draft;
    public bool AiGenerated { get; set; }
    public DateTime? SentAtUtc { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
}
