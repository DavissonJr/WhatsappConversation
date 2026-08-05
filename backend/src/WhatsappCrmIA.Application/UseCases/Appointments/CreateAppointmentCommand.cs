using MediatR;
using Microsoft.EntityFrameworkCore;
using WhatsappCrmIA.Application.Interfaces;
using WhatsappCrmIA.Domain.Common;
using WhatsappCrmIA.Domain.Entities;
using WhatsappCrmIA.Domain.Enums;

namespace WhatsappCrmIA.Application.UseCases.Appointments;

/// <summary>
/// Cria um agendamento e já programa os lembretes automáticos (ex: 1 dia antes,
/// 2 horas antes). Cada lembrete vira um job do Hangfire que dispara sozinho
/// na hora certa, mesmo sem ninguém com o painel aberto.
/// </summary>
public record CreateAppointmentCommand(
    Guid WhatsAppConnectionId,
    string PhoneNumber,
    string? ContactName,
    string Title,
    DateTime ScheduledForUtc,
    string? Notes,
    IReadOnlyList<int> ReminderOffsetMinutes,
    string? ReminderMessageTemplate
) : IRequest<CreateAppointmentResult>;

public record CreateAppointmentResult(
    Guid? AppointmentId, int RemindersScheduled, int RemindersSkippedPast);

public class CreateAppointmentHandler : IRequestHandler<CreateAppointmentCommand, CreateAppointmentResult>
{
    private const string DefaultTemplate =
        "Olá {nome}! Passando para lembrar do seu compromisso \"{titulo}\" em {data} às {hora}. Até lá!";

    private readonly IApplicationDbContext _db;
    private readonly IReminderScheduler _scheduler;

    public CreateAppointmentHandler(IApplicationDbContext db, IReminderScheduler scheduler)
    {
        _db = db;
        _scheduler = scheduler;
    }

    public async Task<CreateAppointmentResult> Handle(CreateAppointmentCommand request, CancellationToken ct)
    {
        var connection = await _db.WhatsAppConnections
            .FirstOrDefaultAsync(w => w.Id == request.WhatsAppConnectionId, ct);
        if (connection is null) return new CreateAppointmentResult(null, 0, 0);

        var normalizedPhone = PhoneNumberNormalizer.Normalize(request.PhoneNumber);
        var contact = await _db.Contacts.FirstOrDefaultAsync(c => c.PhoneNumber == normalizedPhone, ct);

        if (contact is null)
        {
            contact = new Contact { PhoneNumber = normalizedPhone, Name = request.ContactName };
            _db.Contacts.Add(contact);
        }

        var appointment = new Appointment
        {
            Contact = contact,
            WhatsAppConnectionId = connection.Id,
            Title = request.Title,
            ScheduledForUtc = request.ScheduledForUtc,
            Notes = request.Notes,
            Status = AppointmentStatus.Scheduled
        };
        _db.Appointments.Add(appointment);

        var template = string.IsNullOrWhiteSpace(request.ReminderMessageTemplate)
            ? DefaultTemplate
            : request.ReminderMessageTemplate!;

        var scheduled = 0;
        var skippedPast = 0;

        foreach (var minutesBefore in request.ReminderOffsetMinutes.Distinct())
        {
            var sendAt = request.ScheduledForUtc.AddMinutes(-minutesBefore);
            if (sendAt <= DateTime.UtcNow)
            {
                skippedPast++;
                continue; // não agenda lembrete pro passado
            }

            var reminder = new Reminder
            {
                Appointment = appointment,
                SendAtUtc = sendAt,
                Channel = ReminderChannel.WhatsApp,
                Status = ReminderStatus.Pending,
                MessageTemplate = template
            };

            // O Id já existe (gerado no construtor da entidade), então dá pra
            // agendar o job antes mesmo de salvar no banco.
            reminder.HangfireJobId = _scheduler.Schedule(reminder.Id, sendAt);

            _db.Reminders.Add(reminder);
            scheduled++;
        }

        await _db.SaveChangesAsync(ct);
        return new CreateAppointmentResult(appointment.Id, scheduled, skippedPast);
    }
}
