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
    public async Task<IActionResult> Create([FromBody] CreateConnectionRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateWhatsAppConnectionCommand(request.Label), ct);
        return result.Success ? Ok(result.Connection) : BadRequest(new { message = result.Error });
    }

    [HttpGet("{id:guid}/qrcode")]
    public async Task<IActionResult> GetQrCode(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetQrCodeQuery(id), ct);
        return result.Success ? Ok(new { qrCodeBase64 = result.QrCodeBase64 }) : BadRequest(new { message = result.Error });
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
        var result = await _mediator.Send(new DisconnectWhatsAppConnectionCommand(id), ct);
        return result.Success ? Ok() : BadRequest(new { message = result.Error });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var success = await _mediator.Send(new DeleteWhatsAppConnectionCommand(id), ct);
        return success ? Ok() : NotFound();
    }
}
