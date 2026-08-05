namespace WhatsappCrmIA.Application.DTOs;

public record ReminderDto(Guid Id, DateTime SendAtUtc, string Status);

public record AppointmentDto(
    Guid Id,
    ContactDto Contact,
    string WhatsAppConnectionLabel,
    string Title,
    DateTime ScheduledForUtc,
    string Status,
    string? Notes,
    IReadOnlyList<ReminderDto> Reminders);
