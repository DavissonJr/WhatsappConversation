using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WhatsappCrmIA.Application.DTOs;
using WhatsappCrmIA.Application.UseCases.Team;

namespace WhatsappCrmIA.Api.Controllers;

public record InviteTeamMemberRequest(string FullName, string Email, string TemporaryPassword);
public record SetTeamMemberActiveRequest(bool IsActive);

[ApiController]
[Route("api/team")]
[Authorize]
public class TeamController : ControllerBase
{
    private readonly IMediator _mediator;
    public TeamController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TeamMemberDto>>> GetAll(CancellationToken ct)
        => Ok(await _mediator.Send(new GetTeamQuery(), ct));

    [HttpPost]
    public async Task<IActionResult> Invite([FromBody] InviteTeamMemberRequest request, CancellationToken ct)
    {
        var (success, error) = await _mediator.Send(
            new InviteTeamMemberCommand(request.FullName, request.Email, request.TemporaryPassword), ct);
        return success ? Ok() : BadRequest(new { message = error });
    }

    [HttpPut("{id:guid}/active")]
    public async Task<IActionResult> SetActive(Guid id, [FromBody] SetTeamMemberActiveRequest request, CancellationToken ct)
    {
        var success = await _mediator.Send(new SetTeamMemberActiveCommand(id, request.IsActive), ct);
        return success ? Ok() : BadRequest(new { message = "Não foi possível atualizar esse membro." });
    }
}
