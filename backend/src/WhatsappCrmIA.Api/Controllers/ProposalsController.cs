using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WhatsappCrmIA.Application.DTOs;
using WhatsappCrmIA.Application.UseCases.Proposals;
using WhatsappCrmIA.Domain.Enums;

namespace WhatsappCrmIA.Api.Controllers;

public record GenerateProposalRequest(Guid ConversationId);
public record UpdateProposalRequest(string Title, string Description, decimal? Value);
public record UpdateProposalStatusRequest(ProposalStatus Status);

[ApiController]
[Route("api/proposals")]
[Authorize]
public class ProposalsController : ControllerBase
{
    private readonly IMediator _mediator;
    public ProposalsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProposalDto>>> GetAll(CancellationToken ct)
        => Ok(await _mediator.Send(new GetProposalsQuery(), ct));

    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromBody] GenerateProposalRequest request, CancellationToken ct)
    {
        var (id, error) = await _mediator.Send(new GenerateProposalDraftCommand(request.ConversationId), ct);
        return id is null ? BadRequest(new { message = error }) : Ok(new { id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProposalRequest request, CancellationToken ct)
    {
        var success = await _mediator.Send(new UpdateProposalCommand(id, request.Title, request.Description, request.Value), ct);
        return success ? Ok() : NotFound();
    }

    [HttpPost("{id:guid}/send")]
    public async Task<IActionResult> Send(Guid id, CancellationToken ct)
    {
        var (success, error) = await _mediator.Send(new SendProposalCommand(id), ct);
        return success ? Ok() : BadRequest(new { message = error });
    }

    [HttpPut("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateProposalStatusRequest request, CancellationToken ct)
    {
        var success = await _mediator.Send(new UpdateProposalStatusCommand(id, request.Status), ct);
        return success ? Ok() : NotFound();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var success = await _mediator.Send(new DeleteProposalCommand(id), ct);
        return success ? Ok() : NotFound();
    }
}
