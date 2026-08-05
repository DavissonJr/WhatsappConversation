namespace WhatsappCrmIA.Application.DTOs;

public record ContactDto(Guid Id, string? Name, string PhoneNumber);

public record MessageDto(
    Guid Id,
    string Content,
    string Direction,
    string SentBy,
    bool AiGenerated,
    DateTime CreatedAtUtc);

public record ConversationSummaryDto(
    Guid Id,
    ContactDto Contact,
    string Status,
    DateTime LastMessageAtUtc,
    string? LastMessagePreview);

public record ConversationDetailDto(
    Guid Id,
    ContactDto Contact,
    string Status,
    DateTime LastMessageAtUtc,
    IReadOnlyList<MessageDto> Messages);
