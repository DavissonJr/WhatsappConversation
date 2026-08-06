using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WhatsappCrmIA.Application.DTOs;
using WhatsappCrmIA.Application.UseCases.Admin;

namespace WhatsappCrmIA.Api.Controllers;

public record SetTenantActiveRequest(bool IsActive);

/// <summary>
/// Painel exclusivo de quem administra o SaaS (você) — enxerga todas as
/// empresas cadastradas, não só a própria. Protegido pela policy
/// "PlatformAdmin", que exige a claim platform_admin=true no JWT (ver
/// README para como ativar isso pro seu usuário).
/// </summary>
[ApiController]
[Route("api/admin")]
[Authorize(Policy = "PlatformAdmin")]
public class AdminController : ControllerBase
{
    private readonly IMediator _mediator;
    public AdminController(IMediator mediator) => _mediator = mediator;

    [HttpGet("tenants")]
    public async Task<ActionResult<IReadOnlyList<AdminTenantSummaryDto>>> GetTenants(CancellationToken ct)
        => Ok(await _mediator.Send(new GetAdminTenantsQuery(), ct));

    [HttpPut("tenants/{id:guid}/active")]
    public async Task<IActionResult> SetTenantActive(Guid id, [FromBody] SetTenantActiveRequest request, CancellationToken ct)
    {
        var success = await _mediator.Send(new SetTenantActiveCommand(id, request.IsActive), ct);
        return success ? Ok() : NotFound();
    }
}
