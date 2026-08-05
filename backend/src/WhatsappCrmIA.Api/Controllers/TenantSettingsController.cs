using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WhatsappCrmIA.Application.DTOs;
using WhatsappCrmIA.Application.UseCases.TenantSettings;

namespace WhatsappCrmIA.Api.Controllers;

public record UpdateTenantRequest(string Name, string Segment);

[ApiController]
[Route("api/tenant")]
[Authorize]
public class TenantSettingsController : ControllerBase
{
    private readonly IMediator _mediator;
    public TenantSettingsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<ActionResult<TenantSettingsDto>> Get(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetTenantSettingsQuery(), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateTenantRequest request, CancellationToken ct)
    {
        var success = await _mediator.Send(new UpdateTenantSettingsCommand(request.Name, request.Segment), ct);
        return success ? Ok() : NotFound();
    }
}
