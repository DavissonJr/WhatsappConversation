using MediatR;
using Microsoft.EntityFrameworkCore;
using WhatsappCrmIA.Application.Interfaces;
using WhatsappCrmIA.Domain.Enums;

namespace WhatsappCrmIA.Application.UseCases.Appointments;

public record UpdateAppointmentStatusCommand(Guid AppointmentId, AppointmentStatus Status) : IRequest<bool>;

public class UpdateAppointmentStatusHandler : IRequestHandler<UpdateAppointmentStatusCommand, bool>
{
    private readonly IApplicationDbContext _db;
    private readonly IReminderScheduler _scheduler;

    public UpdateAppointmentStatusHandler(IApplicationDbContext db, IReminderScheduler scheduler)
    {
        _db = db;
        _scheduler = scheduler;
    }

    public async Task<bool> Handle(UpdateAppointmentStatusCommand request, CancellationToken ct)
    {
        var appointment = await _db.Appointments
            .Include(a => a.Reminders)
            .FirstOrDefaultAsync(a => a.Id == request.AppointmentId, ct);
        if (appointment is null) return false;

        appointment.Status = request.Status;

        // Cancelado ou concluído: não faz sentido mais mandar lembrete nenhum.
        if (request.Status is AppointmentStatus.Cancelled or AppointmentStatus.Completed)
        {
            foreach (var reminder in appointment.Reminders.Where(r => r.Status == ReminderStatus.Pending))
            {
                if (!string.IsNullOrEmpty(reminder.HangfireJobId))
                    _scheduler.Cancel(reminder.HangfireJobId);
                reminder.Status = ReminderStatus.Cancelled;
            }
        }

        await _db.SaveChangesAsync(ct);
        return true;
    }
}

public record DeleteAppointmentCommand(Guid AppointmentId) : IRequest<bool>;

public class DeleteAppointmentHandler : IRequestHandler<DeleteAppointmentCommand, bool>
{
    private readonly IApplicationDbContext _db;
    private readonly IReminderScheduler _scheduler;

    public DeleteAppointmentHandler(IApplicationDbContext db, IReminderScheduler scheduler)
    {
        _db = db;
        _scheduler = scheduler;
    }

    public async Task<bool> Handle(DeleteAppointmentCommand request, CancellationToken ct)
    {
        var appointment = await _db.Appointments
            .Include(a => a.Reminders)
            .FirstOrDefaultAsync(a => a.Id == request.AppointmentId, ct);
        if (appointment is null) return false;

        foreach (var reminder in appointment.Reminders.Where(r => r.Status == ReminderStatus.Pending))
        {
            if (!string.IsNullOrEmpty(reminder.HangfireJobId))
                _scheduler.Cancel(reminder.HangfireJobId);
        }

        _db.Appointments.Remove(appointment); // cascade apaga os Reminders
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
