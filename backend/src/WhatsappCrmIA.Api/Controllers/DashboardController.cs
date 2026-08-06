using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WhatsappCrmIA.Application.DTOs;
using WhatsappCrmIA.Application.UseCases.Dashboard;

namespace WhatsappCrmIA.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IMediator _mediator;
    public DashboardController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<ActionResult<DashboardSummaryDto>> Get(CancellationToken ct)
        => Ok(await _mediator.Send(new GetDashboardSummaryQuery(), ct));
}
