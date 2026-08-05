using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WhatsappCrmIA.Application.DTOs;
using WhatsappCrmIA.Application.UseCases.WhatsAppConnections;

namespace WhatsappCrmIA.Api.Controllers;

public record CreateConnectionRequest(string Label);

[ApiController]
[Route("api/whatsapp-connections")]
[Authorize]
public class WhatsAppConnectionsController : ControllerBase
{
    private readonly IMediator _mediator;
    public WhatsAppConnectionsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<WhatsAppConnectionDto>>> GetAll(CancellationToken ct)
        => Ok(await _mediator.Send(new GetWhatsAppConnectionsQuery(), ct));

    [HttpPost]
    public async Task<ActionResult<WhatsAppConnectionDto>> Create(
        [FromBody] CreateConnectionRequest request, CancellationToken ct)
        => Ok(await _mediator.Send(new CreateWhatsAppConnectionCommand(request.Label), ct));

    [HttpGet("{id:guid}/qrcode")]
    public async Task<IActionResult> GetQrCode(Guid id, CancellationToken ct)
    {
        var qr = await _mediator.Send(new GetQrCodeQuery(id), ct);
        return qr is null ? NotFound() : Ok(new { qrCodeBase64 = qr });
    }

    [HttpPost("{id:guid}/refresh-status")]
    public async Task<IActionResult> RefreshStatus(Guid id, CancellationToken ct)
    {
        var isConnected = await _mediator.Send(new RefreshConnectionStatusCommand(id), ct);
        return Ok(new { isConnected });
    }

    [HttpPost("{id:guid}/disconnect")]
    public async Task<IActionResult> Disconnect(Guid id, CancellationToken ct)
    {
        var success = await _mediator.Send(new DisconnectWhatsAppConnectionCommand(id), ct);
        return success ? Ok() : NotFound();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var success = await _mediator.Send(new DeleteWhatsAppConnectionCommand(id), ct);
        return success ? Ok() : NotFound();
    }
}
