using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WhatsappCrmIA.Application.DTOs;
using WhatsappCrmIA.Application.UseCases.Conversations;

namespace WhatsappCrmIA.Api.Controllers;

public record SendMessageRequest(string Content);
public record StartConversationRequest(Guid WhatsAppConnectionId, string PhoneNumber, string? ContactName, string Content);

/// <summary>
/// Endpoints consumidos pelo Inbox do painel (Angular). Requer autenticação;
/// o tenant é resolvido pela claim "tenant_id" do JWT (ver CurrentTenantService).
/// </summary>
[ApiController]
[Route("api/conversations")]
[Authorize]
public class ConversationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ConversationsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ConversationSummaryDto>>> GetAll(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetConversationsQuery(), ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ConversationDetailDto>> GetById(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetConversationByIdQuery(id), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("start")]
    public async Task<IActionResult> StartConversation(
        [FromBody] StartConversationRequest request, CancellationToken ct)
    {
        var conversationId = await _mediator.Send(new StartConversationCommand(
            request.WhatsAppConnectionId, request.PhoneNumber, request.ContactName, request.Content), ct);

        return conversationId is null
            ? BadRequest(new { message = "Número de WhatsApp de origem inválido." })
            : Ok(new { conversationId });
    }

    [HttpPost("{id:guid}/messages")]
    public async Task<IActionResult> SendMessage(Guid id, [FromBody] SendMessageRequest request, CancellationToken ct)
    {
        var success = await _mediator.Send(new SendManualMessageCommand(id, request.Content), ct);
        return success ? Ok() : NotFound();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var success = await _mediator.Send(new DeleteConversationCommand(id), ct);
        return success ? Ok() : NotFound();
    }
}
