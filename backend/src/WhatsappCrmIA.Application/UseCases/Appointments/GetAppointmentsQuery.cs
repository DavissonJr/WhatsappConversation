using MediatR;
using Microsoft.EntityFrameworkCore;
using WhatsappCrmIA.Application.DTOs;
using WhatsappCrmIA.Application.Interfaces;

namespace WhatsappCrmIA.Application.UseCases.Appointments;

public record GetAppointmentsQuery : IRequest<IReadOnlyList<AppointmentDto>>;

public class GetAppointmentsHandler : IRequestHandler<GetAppointmentsQuery, IReadOnlyList<AppointmentDto>>
{
    private readonly IApplicationDbContext _db;
    public GetAppointmentsHandler(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<AppointmentDto>> Handle(GetAppointmentsQuery request, CancellationToken ct)
    {
        var appointments = await _db.Appointments
            .Include(a => a.Contact)
            .Include(a => a.WhatsAppConnection)
            .Include(a => a.Reminders)
            .OrderBy(a => a.ScheduledForUtc)
            .ToListAsync(ct);

        return appointments
            .Select(a => new AppointmentDto(
                a.Id,
                new ContactDto(a.Contact.Id, a.Contact.Name, a.Contact.PhoneNumber, a.Contact.ProfilePictureUrl),
                a.WhatsAppConnection.Label,
                a.Title,
                a.ScheduledForUtc,
                a.Status.ToString(),
                a.Notes,
                a.Reminders
                    .OrderBy(r => r.SendAtUtc)
                    .Select(r => new ReminderDto(r.Id, r.SendAtUtc, r.Status.ToString()))
                    .ToList()))
            .ToList();
    }
}
