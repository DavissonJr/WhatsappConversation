using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WhatsappCrmIA.Application.DTOs;
using WhatsappCrmIA.Application.UseCases.AiUsage;

namespace WhatsappCrmIA.Api.Controllers;

/// <summary>
/// Só leitura: mostra quantos tokens/custo estimado o tenant consumiu através
/// do nosso app. A Anthropic não expõe uma API pública pra consultar o saldo
/// real da conta — pra isso, o link no painel manda o tenant direto pro
/// console.anthropic.com, onde ele vê o saldo de verdade e gerencia pagamento.
/// </summary>
[ApiController]
[Route("api/ai-usage")]
[Authorize]
public class AiUsageController : ControllerBase
{
    private readonly IMediator _mediator;
    public AiUsageController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<ActionResult<AiUsageSummaryDto>> Get(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetAiUsageQuery(), ct);
        return result is null ? NotFound() : Ok(result);
    }
}
