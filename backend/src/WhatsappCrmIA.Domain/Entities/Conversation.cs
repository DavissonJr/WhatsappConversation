using WhatsappCrmIA.Domain.Common;
using WhatsappCrmIA.Domain.Enums;

namespace WhatsappCrmIA.Domain.Entities;

public class Conversation : BaseEntity
{
    public Guid ContactId { get; set; }
    public Contact Contact { get; set; } = default!;

    // Por qual número da empresa esse contato está falando
    public Guid WhatsAppConnectionId { get; set; }
    public WhatsAppConnection WhatsAppConnection { get; set; } = default!;

    public ConversationStatus Status { get; set; } = ConversationStatus.Open;
    public ConversationIntent LastDetectedIntent { get; set; } = ConversationIntent.Unknown;
    public DateTime LastMessageAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Resposta que a IA sugeriu mas ainda não foi aprovada/enviada por um humano
    /// (preenchido quando RequireHumanApproval está ativo). Fica null quando não
    /// há nada pendente de revisão.
    /// </summary>
    public string? PendingAiSuggestion { get; set; }

    public ICollection<Message> Messages { get; set; } = new List<Message>();
}

public class Message : BaseEntity
{
    public Guid ConversationId { get; set; }
    public Conversation Conversation { get; set; } = default!;

    public string Content { get; set; } = default!;
    public MessageDirection Direction { get; set; }
    public MessageSender SentBy { get; set; }

    public string? WhatsAppMessageId { get; set; } // id retornado pela Evolution API
    public bool AiGenerated { get; set; }
}
