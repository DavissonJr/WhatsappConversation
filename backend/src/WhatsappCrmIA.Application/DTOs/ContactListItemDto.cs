namespace WhatsappCrmIA.Application.DTOs;

public record ContactListItemDto(
    Guid Id,
    string? Name,
    string PhoneNumber,
    string? ProfilePictureUrl,
    string? Notes,
    bool IsBlocked,
    DateTime CreatedAtUtc,
    DateTime? LastActivityUtc,
    int ConversationCount,
    int AppointmentCount,
    int ProposalCount);
