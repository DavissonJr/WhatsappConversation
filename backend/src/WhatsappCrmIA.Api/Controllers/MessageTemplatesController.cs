using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WhatsappCrmIA.Application.DTOs;
using WhatsappCrmIA.Application.UseCases.MessageTemplates;
using WhatsappCrmIA.Domain.Enums;

namespace WhatsappCrmIA.Api.Controllers;

public record CreateTemplateRequest(string Name, TemplateScope Scope, string Content);
public record UpdateTemplateRequest(string Name, TemplateScope Scope, string Content, bool IsActive);

[ApiController]
[Route("api/message-templates")]
[Authorize]
public class MessageTemplatesController : ControllerBase
{
    private readonly IMediator _mediator;
    public MessageTemplatesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MessageTemplateDto>>> GetAll(CancellationToken ct)
        => Ok(await _mediator.Send(new GetMessageTemplatesQuery(), ct));

    [HttpPost]
    public async Task<ActionResult<MessageTemplateDto>> Create(
        [FromBody] CreateTemplateRequest request, CancellationToken ct)
        => Ok(await _mediator.Send(new CreateMessageTemplateCommand(request.Name, request.Scope, request.Content), ct));

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTemplateRequest request, CancellationToken ct)
    {
        var success = await _mediator.Send(
            new UpdateMessageTemplateCommand(id, request.Name, request.Scope, request.Content, request.IsActive), ct);
        return success ? Ok() : NotFound();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var success = await _mediator.Send(new DeleteMessageTemplateCommand(id), ct);
        return success ? Ok() : NotFound();
    }
}
