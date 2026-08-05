using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WhatsappCrmIA.Application.DTOs;
using WhatsappCrmIA.Application.UseCases.Appointments;
using WhatsappCrmIA.Domain.Enums;

namespace WhatsappCrmIA.Api.Controllers;

public record CreateAppointmentRequest(
    Guid WhatsAppConnectionId,
    string PhoneNumber,
    string? ContactName,
    string Title,
    DateTime ScheduledForUtc,
    string? Notes,
    IReadOnlyList<int> ReminderOffsetMinutes,
    string? ReminderMessageTemplate);

public record UpdateAppointmentStatusRequest(AppointmentStatus Status);

[ApiController]
[Route("api/appointments")]
[Authorize]
public class AppointmentsController : ControllerBase
{
    private readonly IMediator _mediator;
    public AppointmentsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AppointmentDto>>> GetAll(CancellationToken ct)
        => Ok(await _mediator.Send(new GetAppointmentsQuery(), ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAppointmentRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateAppointmentCommand(
            request.WhatsAppConnectionId, request.PhoneNumber, request.ContactName, request.Title,
            request.ScheduledForUtc, request.Notes, request.ReminderOffsetMinutes, request.ReminderMessageTemplate), ct);

        if (result.AppointmentId is null)
            return BadRequest(new { message = "Número de WhatsApp inválido." });

        return Ok(new
        {
            id = result.AppointmentId,
            remindersScheduled = result.RemindersScheduled,
            remindersSkippedPast = result.RemindersSkippedPast
        });
    }

    [HttpPut("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateAppointmentStatusRequest request, CancellationToken ct)
    {
        var success = await _mediator.Send(new UpdateAppointmentStatusCommand(id, request.Status), ct);
        return success ? Ok() : NotFound();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var success = await _mediator.Send(new DeleteAppointmentCommand(id), ct);
        return success ? Ok() : NotFound();
    }
}
