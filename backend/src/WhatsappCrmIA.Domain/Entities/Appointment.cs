using WhatsappCrmIA.Domain.Common;
using WhatsappCrmIA.Domain.Enums;

namespace WhatsappCrmIA.Domain.Entities;

public class Appointment : BaseEntity
{
    public Guid ContactId { get; set; }
    public Contact Contact { get; set; } = default!;

    // De qual número da empresa o lembrete deve ser enviado
    public Guid WhatsAppConnectionId { get; set; }
    public WhatsAppConnection WhatsAppConnection { get; set; } = default!;

    public string Title { get; set; } = default!;
    public DateTime ScheduledForUtc { get; set; }
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Scheduled;
    public string? Notes { get; set; }

    public ICollection<Reminder> Reminders { get; set; } = new List<Reminder>();
}

public class Reminder : BaseEntity
{
    public Guid AppointmentId { get; set; }
    public Appointment Appointment { get; set; } = default!;

    public DateTime SendAtUtc { get; set; }
    public ReminderChannel Channel { get; set; } = ReminderChannel.WhatsApp;
    public ReminderStatus Status { get; set; } = ReminderStatus.Pending;
    public string MessageTemplate { get; set; } = default!;

    // Preenchido pelo Hangfire quando o job é agendado
    public string? HangfireJobId { get; set; }
}
