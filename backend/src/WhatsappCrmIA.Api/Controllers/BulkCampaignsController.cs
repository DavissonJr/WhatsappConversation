using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WhatsappCrmIA.Application.DTOs;
using WhatsappCrmIA.Application.UseCases.BulkMessages;

namespace WhatsappCrmIA.Api.Controllers;

public record BulkAudiencePreviewRequest(BulkAudienceFilters Filters);

public record CreateBulkCampaignRequest(
    string Title, string MessageText, Guid WhatsAppConnectionId, int DelaySeconds, BulkAudienceFilters Filters);

[ApiController]
[Route("api/bulk-campaigns")]
[Authorize]
public class BulkCampaignsController : ControllerBase
{
    private readonly IMediator _mediator;
    public BulkCampaignsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BulkCampaignSummaryDto>>> GetAll(CancellationToken ct)
        => Ok(await _mediator.Send(new GetBulkCampaignsQuery(), ct));

    [HttpPost("preview")]
    public async Task<IActionResult> Preview([FromBody] BulkAudiencePreviewRequest request, CancellationToken ct)
    {
        var count = await _mediator.Send(new GetBulkAudiencePreviewQuery(request.Filters), ct);
        return Ok(new { count });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateBulkCampaignRequest request, CancellationToken ct)
    {
        var (id, error) = await _mediator.Send(new CreateBulkCampaignCommand(
            request.Title, request.MessageText, request.WhatsAppConnectionId, request.DelaySeconds, request.Filters), ct);

        return id is null ? BadRequest(new { message = error }) : Ok(new { id });
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        var success = await _mediator.Send(new CancelBulkCampaignCommand(id), ct);
        return success ? Ok() : NotFound();
    }
}
