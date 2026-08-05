namespace WhatsappCrmIA.Application.DTOs;

public record ProposalDto(
    Guid Id,
    ContactDto Contact,
    Guid? ConversationId,
    string Title,
    string Description,
    decimal? Value,
    string Status,
    bool AiGenerated,
    DateTime? SentAtUtc,
    DateTime CreatedAtUtc);
